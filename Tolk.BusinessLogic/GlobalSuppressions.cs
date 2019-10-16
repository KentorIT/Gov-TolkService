// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Ef uses List<> when denoting navigation", Scope = "namespaceanddescendants", Target = "Tolk.BusinessLogic.Entities")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Generated code", Scope = "namespaceanddescendants", Target = "Tolk.BusinessLogic.Data.Migrations")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "This is a swedish system, with english error messages", Scope = "module")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Initial decision is to not do this", Scope = "module")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Genreated code", Scope = "namespaceanddescendants", Target = "Tolk.BusinessLogic.Entities")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Genreated code", Scope = "namespaceanddescendants", Target = "Tolk.BusinessLogic.Data.Migrations")]
