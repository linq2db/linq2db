using LinqToDB.Common;
using LinqToDB.DataProvider;
using LinqToDB.Extensions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Common
{
	[TestFixture]
	public class DataToolsTests
	{
		private static readonly object[] _convertStringToSqlTestData = new object[]
		{
			new object[] { ""        , null   , null, "''"                                               },
			new object[] { ""        , "START", null, "START''"                                          },
			new object[] { "test"    , null   , null, "'test'"                                           },
			new object[] { "test"    , "START", null, "START'test'"                                      },
			new object[] { "\0"      , null   , null, "chr(0)"                                           },
			new object[] { "\0"      , "START", null, "chr(0)"                                           },
			new object[] { "'"       , null   , null, "''''"                                             },
			new object[] { "'"       , "START", null, "START''''"                                        },
			new object[] { "te\0st"  , null   , null, "'te' & chr(0) & 'st'"                             },
			new object[] { "te\0st"  , "START", null, "START'te' & chr(0) & START'st'"                   },
			new object[] { "te'st"   , null   , null, "'te''st'"                                         },
			new object[] { "te'st"   , "START", null, "START'te''st'"                                    },
			new object[] { "te\0\0st", null   , null, "'te' & chr(0) & chr(0) & 'st'"                    },
			new object[] { "te\0\0st", "START", null, "START'te' & chr(0) & chr(0) & START'st'"          },
			new object[] { "te''st"  , null   , null, "'te''''st'"                                       },
			new object[] { "te''st"  , "START", null, "START'te''''st'"                                  },
			new object[] { "te\0"    , null   , null, "'te' & chr(0)"                                    },
			new object[] { "te\0"    , "START", null, "START'te' & chr(0)"                               },
			new object[] { "te'"     , null   , null, "'te'''"                                           },
			new object[] { "te'"     , "START", null, "START'te'''"                                      },
			new object[] { "\0st"    , null   , null, "chr(0) & 'st'"                                    },
			new object[] { "\0st"    , "START", null, "chr(0) & START'st'"                               },
			new object[] { "'te"     , null   , null, "'''te'"                                           },
			new object[] { "'te"     , "START", null, "START'''te'"                                      },
			new object[] { "\0\0"    , null   , null, "chr(0) & chr(0)"                                  },
			new object[] { "\0\0"    , "START", null, "chr(0) & chr(0)"                                  },
			new object[] { "''"      , null   , null, "''''''"                                           },
			new object[] { "''"      , "START", null, "START''''''"                                      },
			new object[] { "\0'\0'\0", null   , null, "chr(0) & '''' & chr(0) & '''' & chr(0)"           },
			new object[] { "\0'\0'\0", "START", null, "chr(0) & START'''' & chr(0) & START'''' & chr(0)" },

			new object[] { "\r"      , null   , new char[] { '\r', '\n' }, "chr(13)"                                        },
			new object[] { "\n"      , "START", new char[] { '\r', '\n' }, "chr(10)"                                        },
			new object[] { "te\rst"  , null   , new char[] { '\r', '\n' }, "'te' & chr(13) & 'st'"                          },
			new object[] { "te\nst"  , "START", new char[] { '\r', '\n' }, "START'te' & chr(10) & START'st'"                },
			new object[] { "te\r\nst", null   , new char[] { '\r', '\n' }, "'te' & chr(13) & chr(10) & 'st'"                },
			new object[] { "te\n\rst", "START", new char[] { '\r', '\n' }, "START'te' & chr(10) & chr(13) & START'st'"      },
			new object[] { "te\r"    , null   , new char[] { '\r', '\n' }, "'te' & chr(13)"                                 },
			new object[] { "te\n"    , "START", new char[] { '\r', '\n' }, "START'te' & chr(10)"                            },
			new object[] { "\rst"    , null   , new char[] { '\r', '\n' }, "chr(13) & 'st'"                                 },
			new object[] { "\nst"    , "START", new char[] { '\r', '\n' }, "chr(10) & START'st'"                            },
			new object[] { "\0\r\0"  , null   , new char[] { '\r', '\n' }, "chr(0) & chr(13) & chr(0)"                      },
			new object[] { "\0\n\0"  , "START", new char[] { '\r', '\n' }, "chr(0) & chr(10) & chr(0)"                      },
			new object[] { "'\r'"    , null   , new char[] { '\r', '\n' }, "'''' & chr(13) & ''''"                          },
			new object[] { "'\n'"    , "START", new char[] { '\r', '\n' }, "START'''' & chr(10) & START''''"                },
		};

		[TestCaseSource(nameof(_convertStringToSqlTestData))]
		public void ConvertStringToSql(string testString, string startPrefix, char[] extraEscapes, string expected)
		{
			var sb = new StringBuilder();

			DataTools.ConvertStringToSql(
				sb,
				"&",
				startPrefix,
				(strb, c) => strb.AppendFormat("chr({0})", ((int)c).ToString(CultureInfo.InvariantCulture)),
				testString,
				extraEscapes);

			Assert.AreEqual(expected, sb.ToString());
		}
	}
}
