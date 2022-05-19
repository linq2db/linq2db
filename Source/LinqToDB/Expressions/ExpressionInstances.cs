using System.Linq.Expressions;

namespace LinqToDB.Expressions;

/// <summary>
/// Contains pre-created instances of <see cref="ConstantExpression"/> object for often used constants.
/// Using those instances we avoid unnecessary allocations of same constant instances and avoid boxing for
/// value constants (e.g. booleans, integers).
/// </summary>
public static class ExpressionInstances
{
	public static readonly ConstantExpression True        = Expression.Constant(true);
	public static readonly ConstantExpression False       = Expression.Constant(false);
	public static readonly ConstantExpression UntypedNull = Expression.Constant(null);
	
	public static readonly ConstantExpression Constant0  = Expression.Constant(0);
	public static readonly ConstantExpression Constant1  = Expression.Constant(1);

	// those constants used by linq2db but not covered by _int32Constants array
	public static readonly ConstantExpression Constant26  = Expression.Constant(26);
	public static readonly ConstantExpression Constant29  = Expression.Constant(29);

	private static readonly ConstantExpression[] _int32Constants = new ConstantExpression[]
	{
		Constant0,
		Constant1,
		Expression.Constant(2),
		Expression.Constant(3),
		Expression.Constant(4),
		Expression.Constant(5),
		Expression.Constant(6),
		Expression.Constant(7),
		Expression.Constant(8),
		Expression.Constant(9),
		Expression.Constant(10),
	};

	private static readonly ConstantExpression[][] _int32Length1Arrays = new ConstantExpression[][]
	{
		new []{ _int32Constants[0] },
		new []{ _int32Constants[1] },
		new []{ _int32Constants[2] },
		new []{ _int32Constants[3] },
		new []{ _int32Constants[4] },
		new []{ _int32Constants[5] },
		new []{ _int32Constants[6] },
		new []{ _int32Constants[7] },
		new []{ _int32Constants[8] },
		new []{ _int32Constants[9] },
		new []{ _int32Constants[10] },
	};

	// integer constants with 0+ values used a lot for indexes (e.g. in array access and data reader expressions)
	internal static ConstantExpression Int32(int value)
	{
		return value >= 0 && value < _int32Constants.Length
			? _int32Constants[value]
			: Expression.Constant(value);
	}

	internal static ConstantExpression[] Int32Array(int value)
	{
		return value >= 0 && value < _int32Constants.Length
			? _int32Length1Arrays[value]
			: new[] { Expression.Constant(value) };
	}

	internal static ConstantExpression Boolean(bool value) => value ? True : False;
}
