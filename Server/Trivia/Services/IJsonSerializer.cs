﻿namespace Trivia.Services
{
    public interface IJsonSerializer
    {
        string Serialize<T>(T value, bool indent = false);
        
        T Deserialize<T>(string json);
    }
}