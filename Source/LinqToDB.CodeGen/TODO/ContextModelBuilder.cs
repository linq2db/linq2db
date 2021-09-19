
//			// database technically could have multiple same FKs with different names
//			// duplicates have no value for mappings as there is no way to say (except maybe hints) which FK
//			// to use
//			var duplicateFKs = new Dictionary<(TableLikeObject from, TableLikeObject to), List<ISet<(Column from, Column to)>>>();

//			foreach (var fk in dataModel.ForeignKeys)
//			{
//				var columnsSet = new HashSet<(Column, Column)>(fk.Relation);

//				if (duplicateFKs.TryGetValue((fk.Source, fk.Target), out var list))
//				{
//					var isDuplicate = false;
//					foreach (var knowKey in list)
//					{
//						if (knowKey.Count == columnsSet.Count)
//						{
//							var differ = false;
//							foreach (var pair in columnsSet)
//							{
//								if (!knowKey.Contains(pair))
//								{
//									differ = true;
//									break;
//								}
//							}

//							if (!differ)
//							{
//								isDuplicate = true;
//								break;
//							}
//						}
//					}

//					// skip duplicate key
//					if (isDuplicate)
//						continue;
//				}
//				else
//				{
//					duplicateFKs.Add((fk.Source, fk.Target), new List<ISet<(Column, Column)>>() { columnsSet });
//				}
