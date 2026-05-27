//|--- String.cs -----------------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

namespace Iona.Builtins;

public class String
{
    public List<Char> Characters => _characters;
    private readonly List<Char> _characters = [];

    public static implicit operator String(string value) => new String(value);
    
    public String(string value)
    {
        foreach (var character in value.ToCharArray())
        {
            _characters.Add(character);
        }
    }

    public String(List<Char> characters)
    {
        _characters = [..characters];
    }

    public String Append(String value)
    {
        var combined = _characters;
        combined.AddRange(value._characters.ToArray());
        
        return new String(combined);
    }
}