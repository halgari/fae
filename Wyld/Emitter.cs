using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using GrEmit;
using Wyld.Expressions;

namespace Wyld
{
    public class Emitter
    {
        private readonly AssemblyName _assemblyName;
        private readonly AssemblyBuilder _assemblyBuilder;
        private ModuleBuilder _moduleBuilder;

        private Dictionary<object, FieldInfo> _nonNativeConstants = new();
        private TypeBuilder _typeBuilder;
        private ConstructorBuilder _staticConstructor;
        private GroboIL _staticConstructorIL;

        public Emitter()
        {  
            _assemblyName = new AssemblyName("RuntimeData");
            _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(_assemblyName, AssemblyBuilderAccess.RunAndCollect);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule(_assemblyName.Name!);
        }

        public IInvokableArity<object> EmitTopLevel(IExpression expr)
        {
            if (expr.Type != typeof(object))
                expr = Expression.Convert<object>(expr);
            
            _typeBuilder = _moduleBuilder.DefineType("TopLevel", TypeAttributes.Class | TypeAttributes.Sealed);
            _typeBuilder.AddInterfaceImplementation(typeof(IInvokableArity<object>));
            _typeBuilder.AddInterfaceImplementation(typeof(IInvokableCombination<IInvokableArity<object>>));
            var mb = _typeBuilder.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.Virtual, typeof(object), Array.Empty<Type>());
            mb.SetReturnType(typeof(object));

            _staticConstructor = _typeBuilder.DefineConstructor(MethodAttributes.Static, CallingConventions.Standard, Array.Empty<Type>());
            _staticConstructorIL = new GroboIL(_staticConstructor);

            _typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

            using (var il = new GroboIL(mb, analyzeStack:true))
            {
                var ws = new WriterState(il, this);
                expr.Emit(ws);
                il.Ret();
            }
            _typeBuilder.DefineMethodOverride(mb, typeof(IInvokableArity<object>).GetMethod("Invoke")!);

            _staticConstructorIL.Ret();
            var newTp = _typeBuilder.CreateType();
            var inst = Activator.CreateInstance(newTp!);
            return (IInvokableArity<object>)inst!;
        }

        public void EmitLambda(Lambda lambda, WriterState parentWriter)
        {
            _typeBuilder = _moduleBuilder.DefineType(lambda.Name, TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Public);
            _staticConstructor = _typeBuilder.DefineConstructor(MethodAttributes.Static, CallingConventions.Standard, Array.Empty<Type>());
            _staticConstructorIL = new GroboIL(_staticConstructor);
            _typeBuilder.AddInterfaceImplementation(lambda.Type);

            foreach (var arity in lambda.Arities)
            {
                _typeBuilder.AddInterfaceImplementation(arity.Type);

                var mb = _typeBuilder.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.Virtual);
                mb.SetParameters(arity.Parameters.Select(p => p.Type).ToArray());
                mb.SetReturnType(arity.Body.Type);
                using (var il = new GroboIL(mb))
                {
                    var state = new WriterState(il, this);
                    arity.Body.Emit(state);
                    il.Ret();
                }
                _typeBuilder.DefineMethodOverride(mb, arity.Type.GetMethod("Invoke")!);
            }
            _staticConstructorIL.Ret();
            
            var ctor = _typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

            var tp =_typeBuilder.CreateType();
            parentWriter.IL.Newobj(ctor);
        }

        public FieldInfo AddNonNativeConstant(IBox box)
        {
            if (_nonNativeConstants.TryGetValue(box, out var result))
                return result;

            var nm = "const_box_" + _nonNativeConstants.Count;
            var fi = _typeBuilder.DefineField(nm, box.GetType(), FieldAttributes.Static | FieldAttributes.Private);
            _nonNativeConstants.Add(box, fi);
            
            _staticConstructorIL.Ldstr(box.Name.Namespace);
            _staticConstructorIL.Ldstr(box.Name.Name);
            _staticConstructorIL.Call(typeof(Symbol).GetMethod("Intern", new []{typeof(string), typeof(string)}));
            _staticConstructorIL.Ldtoken(box.Type);
            _staticConstructorIL.Call(typeof(Type).GetMethod("GetTypeFromHandle"));
            _staticConstructorIL.Call(typeof(Environment).GetMethod("DefineBox"));
            _staticConstructorIL.Castclass(box.GetType());
            _staticConstructorIL.Stfld(fi);
            return fi;
        }
    }
}