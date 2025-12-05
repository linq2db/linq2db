using System;

namespace LinqToDB.LINQPad;

public sealed class LinqToDBLinqPadException(string message) : Exception(message)
{
}
