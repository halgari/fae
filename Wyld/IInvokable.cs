using System;
using Wyld;

public interface IInvokable 
{
}

public static class Invokable
{
    public static Type InvokableArity(int i) {
        return i switch {
                    0 => typeof(IInvokableArity<>),
            1 => typeof(IInvokableArity<,>),
            2 => typeof(IInvokableArity<,,>),
            3 => typeof(IInvokableArity<,,,>),
            4 => typeof(IInvokableArity<,,,,>),
            5 => typeof(IInvokableArity<,,,,,>),
            6 => typeof(IInvokableArity<,,,,,,>),
            7 => typeof(IInvokableArity<,,,,,,,>),
            8 => typeof(IInvokableArity<,,,,,,,,>),
            9 => typeof(IInvokableArity<,,,,,,,,,>),
            _ => throw new Exception($"Cannot create a InvokableArity of {i} arguments")
        };  

    }


    public static Type InvokableCombination(int i) {
        return i switch {
                    0 => typeof(IInvokableCombination<>),
            1 => typeof(IInvokableCombination<>),
            2 => typeof(IInvokableCombination<,>),
            3 => typeof(IInvokableCombination<,,>),
            4 => typeof(IInvokableCombination<,,,>),
            5 => typeof(IInvokableCombination<,,,,>),
            6 => typeof(IInvokableCombination<,,,,,>),
            7 => typeof(IInvokableCombination<,,,,,,>),
            8 => typeof(IInvokableCombination<,,,,,,,>),
            9 => typeof(IInvokableCombination<,,,,,,,,>),
            _ => throw new Exception($"Cannot create a Invokable of {i} arguments")
        };  

    }


    public static Type Action(int itms) {
        return itms switch {
            0 => typeof(Action),
        
            1 => typeof(Action<>),

            2 => typeof(Action<,>),

            3 => typeof(Action<,,>),

            4 => typeof(Action<,,,>),

            5 => typeof(Action<,,,,>),

            6 => typeof(Action<,,,,,>),

            7 => typeof(Action<,,,,,,>),

            8 => typeof(Action<,,,,,,,>),

            9 => typeof(Action<,,,,,,,,>),

            10 => typeof(Action<,,,,,,,,,>),

            11 => typeof(Action<,,,,,,,,,,>),

            12 => typeof(Action<,,,,,,,,,,,>),

            13 => typeof(Action<,,,,,,,,,,,,>),
            _ => throw new Exception($"Cannot create a Invokable of {itms} arguments")
        };  

    }


}

public interface IInvokableArity<TR> : IInvokable {
   Result<TR> Invoke();
}


public interface IInvokableArity<TR, T0> : IInvokable {
   Result<TR> Invoke(T0 arg0);
}


public interface IInvokableArity<TR, T0, T1> : IInvokable {
   Result<TR> Invoke(T0 arg0, T1 arg1);
}


public interface IInvokableArity<TR, T0, T1, T2> : IInvokable {
   Result<TR> Invoke(T0 arg0, T1 arg1, T2 arg2);
}


public interface IInvokableArity<TR, T0, T1, T2, T3> : IInvokable {
   Result<TR> Invoke(T0 arg0, T1 arg1, T2 arg2, T3 arg3);
}


public interface IInvokableArity<TR, T0, T1, T2, T3, T4> : IInvokable {
   Result<TR> Invoke(T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4);
}


public interface IInvokableArity<TR, T0, T1, T2, T3, T4, T5> : IInvokable {
   Result<TR> Invoke(T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
}


public interface IInvokableArity<TR, T0, T1, T2, T3, T4, T5, T6> : IInvokable {
   Result<TR> Invoke(T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
}


public interface IInvokableArity<TR, T0, T1, T2, T3, T4, T5, T6, T7> : IInvokable {
   Result<TR> Invoke(T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
}


public interface IInvokableArity<TR, T0, T1, T2, T3, T4, T5, T6, T7, T8> : IInvokable {
   Result<TR> Invoke(T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
}


public interface IInvokableCombination<T0> : IInvokable 

   where T0 : IInvokable

{
}

public interface IInvokableCombination<T0, T1> : IInvokable 

   where T0 : IInvokable
   where T1 : IInvokable

{
}

public interface IInvokableCombination<T0, T1, T2> : IInvokable 

   where T0 : IInvokable
   where T1 : IInvokable
   where T2 : IInvokable

{
}

public interface IInvokableCombination<T0, T1, T2, T3> : IInvokable 

   where T0 : IInvokable
   where T1 : IInvokable
   where T2 : IInvokable
   where T3 : IInvokable

{
}

public interface IInvokableCombination<T0, T1, T2, T3, T4> : IInvokable 

   where T0 : IInvokable
   where T1 : IInvokable
   where T2 : IInvokable
   where T3 : IInvokable
   where T4 : IInvokable

{
}

public interface IInvokableCombination<T0, T1, T2, T3, T4, T5> : IInvokable 

   where T0 : IInvokable
   where T1 : IInvokable
   where T2 : IInvokable
   where T3 : IInvokable
   where T4 : IInvokable
   where T5 : IInvokable

{
}

public interface IInvokableCombination<T0, T1, T2, T3, T4, T5, T6> : IInvokable 

   where T0 : IInvokable
   where T1 : IInvokable
   where T2 : IInvokable
   where T3 : IInvokable
   where T4 : IInvokable
   where T5 : IInvokable
   where T6 : IInvokable

{
}

public interface IInvokableCombination<T0, T1, T2, T3, T4, T5, T6, T7> : IInvokable 

   where T0 : IInvokable
   where T1 : IInvokable
   where T2 : IInvokable
   where T3 : IInvokable
   where T4 : IInvokable
   where T5 : IInvokable
   where T6 : IInvokable
   where T7 : IInvokable

{
}

public interface IInvokableCombination<T0, T1, T2, T3, T4, T5, T6, T7, T8> : IInvokable 

   where T0 : IInvokable
   where T1 : IInvokable
   where T2 : IInvokable
   where T3 : IInvokable
   where T4 : IInvokable
   where T5 : IInvokable
   where T6 : IInvokable
   where T7 : IInvokable
   where T8 : IInvokable

{
}

