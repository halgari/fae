using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
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
        private HashSet<FreeVariable> UsedFreeVars { get; } = new();

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
            var mb = _typeBuilder.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.Virtual);
            mb.SetParameters(Array.Empty<Type>());
            mb.SetReturnType(typeof(Result<object>));

            _staticConstructor = _typeBuilder.DefineConstructor(MethodAttributes.Static, CallingConventions.Standard, Array.Empty<Type>());
            _staticConstructorIL = new GroboIL(_staticConstructor);

            _typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
            

            using (var il = new GroboIL(mb, analyzeStack:true))
            {
                var ws = new WriterState(il, this, typeof(object));
                ws.InvokeK = SetupK(mb.ReturnType);
                expr.Emit(ws);
                EmitWrapInResult(ws, expr.Type);
                il.Ret();
                ws.EmitKBuilders(Array.Empty<ILocal>());
                EmitInvokeK(ws, expr, Array.Empty<ILocal>());
            }
            _typeBuilder.DefineMethodOverride(mb, typeof(IInvokableArity<object>).GetMethod("Invoke")!);

            _staticConstructorIL.Ret();
            var newTp = _typeBuilder.CreateType();
            var inst = Activator.CreateInstance(newTp!);
            return (IInvokableArity<object>)inst!;
        }

        private MethodBuilder SetupK(Type returnType)
        {
            var mi = _typeBuilder.DefineMethod("InvokeK", MethodAttributes.Public | MethodAttributes.Static, returnType,
                new[] {typeof(Effect), typeof(object)});

            
            
            return mi;
        }


        private void EmitWrapInResult(WriterState state, Type valueType)
        {
            
            var type = typeof(Result<>).MakeGenericType(valueType);
            var lcl = state.IL.DeclareLocal(type);
            var tmp = state.IL.DeclareLocal(valueType);
            
            // Save the ret value for later
            state.IL.Stloc(tmp);
            
            // Init the struct
            state.IL.Ldloca(lcl);
            state.IL.Initobj(type);
            
            // Write the value
            state.IL.Ldloca(lcl);
            state.IL.Ldloc(tmp);
            state.IL.Stfld(type.GetField("Value"));
            
            // Push the struct onto the stack
            state.IL.Ldloc(lcl);
            
        }

        public Action<WriterState> EmitLambda(Lambda lambda, WriterState parentWriter)
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
                mb.SetReturnType(typeof(Result<>).MakeGenericType(arity.Body.Type));
                using (var il = new GroboIL(mb))
                {
                    var state = new WriterState(il, this, arity.Body.Type);
                    state.InvokeK = SetupK(mb.ReturnType);
                    arity.Body.Emit(state);
                    EmitWrapInResult(state, arity.Body.Type);
                    il.Ret();
                    var parameters = arity.Parameters
                        .Concat(UsedFreeVars.OfType<ILocal>())
                        .Concat(new [] {(Parameter)lambda.ThisParam})
                        .ToArray();
                    state.EmitKBuilders(parameters);

                    EmitInvokeK(state, arity.Body, parameters);
                    
                }
                _typeBuilder.DefineMethodOverride(mb, arity.Type.GetMethod("Invoke")!);
            }
            _staticConstructorIL.Ret();
            
            var ctor = _typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
            var tp = _typeBuilder.CreateType();
            
            return (wtr) =>
            {

                wtr.IL.Newobj(ctor);

                foreach (var freeVar in UsedFreeVars)
                {
                    wtr.IL.Dup();
                    freeVar.Source.Emit(wtr);
                    wtr.IL.Stfld(freeVar.FieldInfo);
                }
            };

        }

        private void EmitInvokeK(WriterState mainState, IExpression arityBody, ILocal[] parameters)
        {

            using var ik = new GroboIL(mainState.InvokeK);
            var ikstate = new WriterState(ik, this, mainState.FinalResultType);
            ikstate.InvokeK = mainState.InvokeK;
            ikstate.EmittingInvokeK = true;
            
            if (mainState.KBuilders.Count == 0)
            {
                ik.Newobj(typeof(NotImplementedException).GetConstructor(Array.Empty<Type>()));
                ik.Throw();
                return;
            }

            foreach (var kb in mainState.KBuilders)
            {
                kb.UnpackLabel = ik.DefineLabel("unpack");
            }
            
            ik.Ldarg(0);
            ik.Ldfld(Consts.EffectKStateField);
            ik.Castclass(typeof(KState));
            ik.Ldfld(typeof(KState).GetField("StateIdx"));
            ik.Switch(mainState.KBuilders.Select(b => b.UnpackLabel).ToArray());
            

            // Emit unpacking blocks
            foreach (var kb in mainState.KBuilders)
            {
                ik.MarkLabel(kb.UnpackLabel);
                
                // Check if a parent exists
                ik.Ldarg(0);
                ik.Ldfld(Consts.EffectParentField);
                ik.Ldnull();

                var nullParent = ik.DefineLabel("null_parent"); // Resume execution ->>
                ik.Beq(nullParent);
                
                // Call the parent
                
                ik.Ldarg(0);
                ik.Ldfld(Consts.EffectParentField);
                ik.Ldfld(Consts.EffectKField);
                ik.Castclass(kb.LocalResultResumeType);
                
                ik.Ldarg(0);
                ik.Ldfld(Consts.EffectParentField);
                
                ik.Ldarg(1);
                
                ik.Call(kb.LocalResultResumeMethod);

                var resumeLocalTmp = ik.DeclareLocal(kb.LocalResultResultType, "resumeTmp");
                ik.Stloc(resumeLocalTmp);
                ik.Ldloca(resumeLocalTmp);
                
                ik.Ldfld(kb.LocalResultResultEffectField);
                ik.Ldnull();

                var parentGaveValue = ik.DefineLabel("parent_gave_value");
                ik.Beq(parentGaveValue);
                
                ik.Ldarg(0);
                ik.Ldfld(Consts.EffectKStateField);
                
                ik.Ldarg(0);
                ik.Ldfld(Consts.EffectKField);
                
                ik.Ldloca(resumeLocalTmp);
                ik.Ldfld(kb.LocalResultResultEffectField);
                
                
                ik.Call(typeof(Runtime).GetMethod("BuildK")!.MakeGenericMethod(kb.FinalResultType));
                ik.Ret();
                
                ik.MarkLabel(parentGaveValue);
                
                ik.Ldloca(resumeLocalTmp);
                ik.Ldfld(kb.LocalResultResultValueField);
                var castedResultValue = ik.DeclareLocal(kb.LocalResultType, "castedLocalResult");
                ik.Stloc(castedResultValue);
                
                var valueUnwrapped = ik.DefineLabel("value_unwrapped");
                ik.Br(valueUnwrapped);
                

                ik.MarkLabel(nullParent);
                ik.Ldarg(1);
                ConvertStackItem(ik, typeof(object),kb.LocalResultType);
                ik.Stloc(castedResultValue);
                
                ik.MarkLabel(valueUnwrapped);
                ik.Ldarg(0);
                ik.Ldfld(Consts.EffectKStateField);
                var ct = ik.DeclareLocal(kb.StateTupleType, "castedtuple");
                ik.Castclass(kb.StateTupleType);
                ik.Stloc(ct);

                var itmIdx = 0;
                foreach (var param in parameters)
                {
                    ik.Ldloc(ct);
                    ik.Ldfld(kb.StateTupleType.GetField("Item" + itmIdx));
                    var nloc = ikstate.MakeLocalRemap(param);
                    ik.Stloc(nloc);
                    itmIdx++;
                }
                
                foreach (var local in kb.Locals)
                {
                    ik.Ldloc(ct);
                    ik.Ldfld(kb.StateTupleType.GetField("Item" + itmIdx));
                    var nloc = ikstate.MakeLocalRemap(local);
                    ik.Stloc(nloc);

                    itmIdx++;
                }
                
                foreach (var (tp, i) in kb.EvalStack.Select((tp, i) => (tp, i)).Reverse())
                {
                    var fld = kb.StateTupleType.GetField("Item" + itmIdx);
                    ik.Ldloc(ct);
                    ik.Ldfld(fld);
                    itmIdx++;
                }

                kb.ResumeLabel = ik.DefineLabel("resume");
                ik.Ldloc(castedResultValue);
                ik.Br(kb.ResumeLabel);
            }

            ikstate.Resumes = mainState.KBuilders.ToDictionary(b => b.Key);
            arityBody.Emit(ikstate);
            EmitWrapInResult(ikstate, mainState.FinalResultType);
            ik.Ret();
            

            ikstate.EmitKBuilders(parameters);
        }

        private void ConvertStackItem(GroboIL ik, Type from, Type to)
        {
            if (from.IsClass && to.IsPrimitive) {
                ik.Unbox_Any(to);
                return;
            }

            if (from.IsPrimitive && to.IsClass)
            {
                ik.Box(from);
                return;
            }
            
            ik.Castclass(to);
        }

        public FieldInfo AddNonNativeConstant(IBox box)
        {
            if (_nonNativeConstants.TryGetValue(box, out var result))
                return result;

            var nm = "const_box_" + box.Name + _nonNativeConstants.Count;
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

        private int _freeVarIdx;
        public FieldInfo GetFreeVariableField(FreeVariable freeVariable)
        {
            UsedFreeVars.Add(freeVariable);
            if (freeVariable.FieldInfo != null) return freeVariable.FieldInfo;
            string fieldName;
            if (freeVariable.Source is Parameter p)
                fieldName = "_fv_" + p.Name;
            else
            {
                fieldName = "_fv_" + _freeVarIdx++;
            }

            freeVariable.FieldInfo = _typeBuilder.DefineField(fieldName, freeVariable.Type, FieldAttributes.Public);
            return freeVariable.FieldInfo;
        }

        public FieldInfo AddNonNativeConstant(Keyword kw)
        {
            if (_nonNativeConstants.TryGetValue(kw, out var result))
                return result;

            var nm = "const_kw_" + _nonNativeConstants.Count;
            var fi = _typeBuilder.DefineField(nm, kw.GetType(), FieldAttributes.Static | FieldAttributes.Private);
            _nonNativeConstants.Add(kw, fi);
            
            _staticConstructorIL.Ldstr(kw.Namespace);
            _staticConstructorIL.Ldstr(kw.Name);
            _staticConstructorIL.Call(typeof(Keyword).GetMethod("Intern", new []{typeof(string), typeof(string)}));
            _staticConstructorIL.Stfld(fi);
            return fi;
        }
    }
}