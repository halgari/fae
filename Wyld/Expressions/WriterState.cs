using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using GrEmit;

namespace Wyld.Expressions
{
    public class WriterState
    {
        public WriterState(GroboIL il, Emitter emitter, Type finalResultType)
        {
            IL = il;
            Emitter = emitter;
            _tailCallFlags = ImmutableStack<bool>.Empty.Push(true);
            FinalResultType = finalResultType;
        }

        public Type FinalResultType { get; }

        private ImmutableStack<bool> _tailCallFlags;
        private ImmutableStack<Type> _evaluationStack = ImmutableStack<Type>.Empty;

        public IDisposable WithTailCallFlag(bool flag)
        {
            _tailCallFlags = _tailCallFlags.Push(flag);
            return new TailCallFlagPopper(this);
        }

        public bool CanTailCall => _tailCallFlags.Peek();

        public Emitter Emitter { get; }
        public GroboIL IL { get;  }
        
        private class TailCallFlagPopper : IDisposable
        {
            private WriterState _state;

            public TailCallFlagPopper(WriterState writerState)
            {
                _state = writerState;
            }

            public void Dispose()
            {
                _state._tailCallFlags = _state._tailCallFlags.Pop();
            }
        }


        public void EmitResultPostlude(Type type, IExpression key)
        {
            if (CanTailCall)
            {
                IL.Ret();
                return;
            }

            var tp = typeof(Result<>).MakeGenericType(type);
            var tmp = IL.DeclareLocal(tp);
            IL.Stloc(tmp);
            MarkContinuationPostlude(tmp, type, key);
            IL.Ldloca(tmp);
            IL.Ldfld(tp.GetField("Value"));
            
            if (EmittingInvokeK && Resumes.TryGetValue(key, out var kb))
                IL.MarkLabel(kb.ResumeLabel);
        }

        private void MarkContinuationPostlude(GroboIL.Local tmp, Type resultValueType, IExpression key)
        {
            var field = tmp.Type.GetField("Effect");
            var lbl = IL.DefineLabel("build_k");
            IL.Ldloca(tmp);
            IL.Ldfld(field);
            IL.Ldnull();
            var notEffect = IL.DefineLabel("not_effect");
            IL.Beq(notEffect);
            IL.Br(lbl);
            IL.MarkLabel(notEffect);

            MarkKBuilder(tmp, lbl, field, resultValueType, key);

        }


        public List<KBuilder> KBuilders = new();
        public Dictionary<IExpression, KBuilder> Resumes = new();

        private void MarkKBuilder(GroboIL.Local result, GroboIL.Label lbl, FieldInfo field, Type resultValueType, IExpression key)
        {
            KBuilders.Add(new KBuilder
            {
                ResultLocal = result,
                BuilderLabel = lbl,
                EffectField = field,
                EvalStack = EvalStack.ToArray(),
                FinalResultType = FinalResultType,
                LocalResultType = resultValueType,
                Key = key
            });
        }

        public class KBuilder
        {
            /// <summary>
            /// Local that stores the Result<T> struct
            /// </summary>
            public GroboIL.Local ResultLocal;
            
            /// <summary>
            /// Label that starts the Building of the K
            /// </summary>
            public GroboIL.Label BuilderLabel;
            
            /// <summary>
            /// The field that gets Result<T>.Effect
            /// </summary>
            public FieldInfo EffectField;
            
            /// <summary>
            /// The Eval stack at the time of the invoke
            /// </summary>
            public Type[] EvalStack;
            
            /// <summary>
            /// The ultimate return type of the *function* not the current expression
            /// </summary>
            public Type FinalResultType;
            public GroboIL.Label UnpackLabel;
            public Type StateTupleType;
            public GroboIL.Label ResumeLabel;
            public IExpression Key;
            public Type LocalResultType;
        }

        public void EmitEvalArgs(IExpression[] args)
        {
            using var _ = WithTailCallFlag(false);
            foreach (var arg in args)
            {
                arg.Emit(this);
                _evaluationStack = _evaluationStack.Push(arg.Type);
            }
        }

        public void PushToEvalStack(Type tp)
        {
            _evaluationStack = _evaluationStack.Push(tp);
        }

        public void PopFromEvalStack(int argc = 1)
        {
            for (var i = 0; i < argc; i ++) 
                _evaluationStack = _evaluationStack.Pop();
        }

        public IEnumerable<Type> EvalStack => _evaluationStack;
        public MethodBuilder InvokeK { get; set; }
        public bool EmittingInvokeK { get; set; }

        public void EmitKBuilders()
        {
            foreach (var (b, idx) in KBuilders.Select(((builder, i) => (builder, i) )))
            {
                IL.MarkLabel(b.BuilderLabel);

                var stlocals = b.EvalStack.Select((Type, Idx) => new {Type, Idx, Local = IL.DeclareLocal(Type)}).ToArray();
                foreach (var itm in stlocals)
                {
                    IL.Stloc(itm.Local);
                }

                var stateTuple = KStates.KState(stlocals.Length);

                if (stlocals.Length > 0)
                    stateTuple = stateTuple.MakeGenericType(stlocals.Select(t => t.Type).ToArray());
                b.StateTupleType = stateTuple;

                IL.Newobj(stateTuple.GetConstructor(Array.Empty<Type>()));
                IL.Dup();
                IL.Ldc_I4(idx);
                IL.Stfld(stateTuple.GetField("StateIdx"));

                foreach (var local in stlocals)
                {
                    IL.Dup();
                    IL.Ldloc(local.Local);
                    IL.Stfld(stateTuple.GetField("Item"+local.Idx));
                }
                
                
                IL.Ldnull();
                IL.Ldftn(InvokeK);
                var resType = typeof(Result<>).MakeGenericType(b.FinalResultType);
                var ftemplate = typeof(Func<,,>).MakeGenericType(typeof(object), typeof(object), resType);
                var ctor = (ftemplate.GetConstructor(new[] {typeof(object), typeof(IntPtr)}));
                IL.Newobj(ctor);
                IL.Ldloca(b.ResultLocal);

                IL.Ldfld(b.EffectField);
                IL.Call(typeof(Runtime).GetMethod("BuildK")!.MakeGenericMethod(b.FinalResultType));
                IL.Ret();
            }
        }

    }


}