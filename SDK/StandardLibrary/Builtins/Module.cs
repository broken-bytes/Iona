//|--- Module.cs -----------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace Iona.Builtins;

public static class Module
{
    public static void Print(string message)
    {
        Console.Write(message);
    }

    public static void PrintLn(string message)
    {
        Console.WriteLine(message);
    }

    public static void PrintErr(string message)
    {
        Console.Error.Write(message);
    }

    public static void PrintErrLn(string message)
    {
        Console.Error.WriteLine(message);
    }

    public static char Read()
    {
        return Console.ReadKey(true).KeyChar;
    }

    public static string? ReadLn() => Console.ReadLine();
}
