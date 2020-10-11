﻿﻿using System;

  namespace Trivial.Services
{
    public interface IDateProvider
    {
        DateTimeOffset Now { get; }
    }
}
