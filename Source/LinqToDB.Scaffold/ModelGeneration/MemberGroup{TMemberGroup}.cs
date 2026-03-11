using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public abstract class MemberGroup<TMemberGroup> : MemberBase, IMemberGroup
		where TMemberGroup : MemberGroup<TMemberGroup>
	{
		public string?            Region          { get; set; }
		public bool               IsCompact       { get; set; }
		public bool               IsPropertyGroup { get; set; }
		public List<IClassMember> Members         { get; set; } = [];
		public List<string>       Errors          { get; set; } = [];

		public override int  CalcBodyLen() { return 0; }

		public override void Render(ModelGenerator tt, bool isCompact)
		{
			if (!string.IsNullOrEmpty(Region))
			{
				tt.BeginRegion(Region);
				tt.WriteLine("");
			}

			BeginConditional(tt, isCompact);

			if (Errors.Count > 0 && tt.GenerateProcedureErrors)
			{
				tt.RemoveSpace();
				tt.WriteComment(" Use 'GenerateProcedureErrors=false' to disable errors.");
				foreach (var error in Errors)
				{
					tt.Error(error);

					foreach (var e in error.Split('\n'))
					{
						tt.RemoveSpace();
						tt.WriteLine("#error " + e.Trim('\r'));
					}
				}

				tt.WriteLine("");
			}

			if (IsCompact)
			{
				var allMembers = tt.GetTreeNodes(this).OfType<MemberBase>().Where(m => m is not IMemberGroup).ToList();

				if (allMembers.Count > 0)
				{
					var max = allMembers.Max(m => m.AccessModifier == AccessModifier.None ? 0 : m.AccessModifier.ToString().Length);
					foreach (var m in allMembers)
						m.AccessModifierLen = max;

					max = allMembers.Max(m => m.CalcModifierLen());
					foreach (var m in allMembers)
						m.ModifierLen = max;

					max = allMembers.Max(m => (m.BuildType() ?? "").Length);
					foreach (var m in allMembers)
						m.TypeLen = max;

					var notHasGetter = allMembers.OfType<IProperty>().Any(m => m.IsAuto && !m.HasGetter);
					var notHasSetter = allMembers.OfType<IProperty>().Any(m => m.IsAuto && !m.HasSetter);

					foreach (var p in allMembers.OfType<IProperty>())
					{
						if (notHasGetter) p.GetterLen = 13;
						if (notHasSetter) p.SetterLen = 13;
					}

					max = allMembers.Max(m => m.Name?.Length ?? 0);
					foreach (var m in allMembers)
						m.NameLen = max;

					max = allMembers.Max(m => m.CalcParamLen());
					foreach (var m in allMembers)
						m.ParamLen = max;

					max = allMembers.Max(m => m.CalcBodyLen());
					foreach (var m in allMembers)
						m.BodyLen = max;

					var members =
					(
						from m in allMembers
						select new
						{
							m,
							attrs = m.Attributes
								.GroupBy(a => a.Name, StringComparer.Ordinal)
								.SelectMany(gr => gr.Select((a,i) => new { a, name = a.Name + "." + i.ToString(CultureInfo.InvariantCulture) }))
								.Select(a => ToNullable(a))
								.ToList(),
						}
					).ToList();

					T? ToNullable<T>(T obj) where T : class => (T?)obj;

					var attrWeight = members
						.SelectMany(m => m.attrs)
						.GroupBy(
							a => a.name,
							(k, gr) => new { Key = k, Count = gr.Count() },
							StringComparer.Ordinal
						)
						.ToDictionary(a => a.Key, a => a.Count, StringComparer.Ordinal);

					var q =
						from m in members
						where m.attrs.Count > 0
						select new { m, w = m.attrs.Sum(aa => attrWeight[aa.name]) } into m
						orderby m.w descending
						select m.m;

					var attrs = new List<string>();

					foreach (var m in q)
					{
						var list = m.attrs.Select(a => a.name).ToList();

						if (attrs.Count == 0)
							attrs.AddRange(list);
						else
						{
							for (var i = 0; i < list.Count; i++)
							{
								var nm = list[i];

								if (!attrs.Contains(nm, StringComparer.Ordinal))
								{
									for (var j = i + 1; j < list.Count; j++)
									{
										var idx = attrs.IndexOf(list[j]);

										if (idx >= 0)
										{
											attrs.Insert(idx, nm);
											break;
										}
									}
								}

								if (!attrs.Contains(nm, StringComparer.Ordinal))
									attrs.Add(nm);
							}
						}
					}

					var mms = members.Select(m =>
					{
						var arr = new IAttribute?[attrs.Count];

						foreach (var a in m.attrs)
							arr[attrs.IndexOf(a.name)] = a.a;

						return new { m.m, attrs = arr.ToList() };
					}).ToList();

					var idxs = Enumerable.Range(0, attrs.Count).Select(_ => (List<int?>?)new List<int?>()).ToList();

					for (var i = 0; i < mms.Count; i++)
						for (var j = 0; j < mms[i].attrs.Count; j++)
							if (mms[i].attrs[j] != null)
								idxs[j]!.Add(i);

					var toRemove = new List<int>();

					for (var i = 1; i < idxs.Count; i++)
					{
						for (var j = 0; j < i; j++)
						{
							if (idxs[j] == null)
								continue;

							if (!idxs[i]!.Intersect(idxs[j]!).Any())
							{
								foreach (var m in mms)
								{
									if (m.attrs[i] != null)
									{
										m.attrs[j] = m.attrs[i];
										m.attrs[i] = null;
									}
								}

								idxs[j]!.AddRange(idxs[i]!);
								idxs[i] = null;
								toRemove.Add(i);
								break;
							}
						}

					}

					foreach (var n in toRemove.OrderByDescending(i => i))
						foreach (var m in mms)
							m.attrs.RemoveAt(n);

					var lens = new int[attrs.Count - toRemove.Count];

					foreach (var m in mms)
					{
						for (var i = 0; i < m.attrs.Count; i++)
						{
							var a = m.attrs[i];

							if (a != null)
							{
								var len = a.Name?.Length ?? 0;

								if (a.Parameters.Count >= 0)
									len += a.Parameters.Sum(p => 2 + p.Length);

								lens[i] = Math.Max(lens[i], len);
							}
						}
					}

					foreach (var m in allMembers)
					{
						if (m is not IMemberGroup)
							m.BeginConditional(tt, IsCompact);

						foreach (var c in m.Comment)
							tt.WriteComment(c);

						if (attrs.Count > 0)
						{
							var ma = mms.First(mr => mr.m == m);

							if (m.Attributes.Count > 0)
							{
								tt.Write("[");

								for (var i = 0; i < ma.attrs.Count; i++)
								{
									var a = ma.attrs[i];

									if (a == null)
									{
										tt.WriteSpaces(lens[i]);
										if (i + 1 < ma.attrs.Count)
											tt.Write("  ");
									}
									else
									{
										var len = tt.GenerationEnvironment.Length;
										a.Render(tt);
										len = (tt.GenerationEnvironment.Length - len);

										var commaAdded = false;

										for (var j = i + 1; j < ma.attrs.Count; j++)
										{
											if (ma.attrs[j] != null)
											{
												tt.SkipSpacesAndInsert(", ");
												commaAdded = true;
												break;
											}
										}

										if (i + 1 < ma.attrs.Count && !commaAdded)
											tt.Write("  ");

										tt.WriteSpaces(lens[i] - len);
									}
								}

								tt.Write("] ");
							}
							else
							{
								tt.WriteSpaces(lens.Sum() + ma.attrs.Count * 2 + 1);
							}
						}

						m.Render(tt, true);

						if (!IsCompact)
							tt.WriteLine("");

						if (m is not IMemberGroup)
							m.EndConditional(tt, IsCompact);
					}
				}
			}
			else
			{
				foreach (var cm in Members)
				{
					if (cm is MemberBase m)
					{
						if (m is not IMemberGroup)
							m.BeginConditional(tt, IsCompact);

						foreach (var c in m.Comment)
							tt.WriteComment(c);

						if (m.Attributes.Count > 0)
						{
							var q = m.Attributes.GroupBy(a => a.Conditional ?? "", StringComparer.Ordinal);

							foreach (var g in q)
							{
								if (g.Key.Length > 0)
								{
									tt.RemoveSpace();
									tt.WriteLine($"#if {g.Key}");
								}

								var attrs = g.ToList();

								var aa = attrs.Where(a => !a.IsSeparated).ToList();

								if (aa.Count > 0)
								{
									tt.Write("[");

									for (var i = 0; i < aa.Count; i++)
									{
										if (i > 0) tt.Write(", ");
										aa[i].Render(tt);
									}

									tt.WriteLine("]");
								}

								aa = attrs.Where(a => a.IsSeparated).ToList();

								foreach (var a in aa)
								{
									tt.Write("[");
									a.Render(tt);
									tt.WriteLine("]");
								}

								if (g.Key.Length > 0)
								{
									tt.RemoveSpace();
									tt.WriteLine("#endif");
								}
							}
						}

						m.Render(tt, false);

						if (m.InsertBlankLineAfter)
							tt.WriteLine("");

						if (m is not IMemberGroup)
							m.EndConditional(tt, IsCompact);
					}
					else if (cm is TypeBase t)
					{
						t.Render(tt);
						tt.WriteLine("");
					}
				}
			}

			tt.Trim();

			EndConditional(tt, isCompact);

			if (!string.IsNullOrEmpty(Region))
			{
				tt.WriteLine("");
				tt.EndRegion();
			}
		}

		public override IEnumerable<ITree> GetNodes() { return Members; }

		public override void SetTree()
		{
			foreach (var ch in GetNodes())
			{
				ch.Parent = this;
				ch.SetTree();
			}
		}
	}
}
