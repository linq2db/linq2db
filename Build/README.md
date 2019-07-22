This directory contains MSBUILD props files, used by solution projects

### All projects
- linq2db.Default.props - default props, used by all projects. Contains generic properties, that are used by all projects.

### Product projects
- linq2db.Source.props - props for `LinqToDB` and `LinqToDB.Tools` projects. Imports linq2db.Default.props

###Test Projects
- linq2db.Tests.props - props, used by test projects. Imports linq2db.Default.props
- linq2db.Tests.Providers.props - references to database providers, used by tests, and some other shared properties. Imports linq2db.Tests.props.
