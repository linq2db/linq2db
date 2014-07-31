###############################################################################
#
# provider.ps1 --
#
# Written by Joe Mistachkin.
# Released to the public domain, use at your own risk!
#
###############################################################################

param($installPath, $toolsPath, $package, $project)

Add-EFProvider $project "System.Data.SQLite.EF6" `
    "System.Data.SQLite.EF6.SQLiteProviderServices, System.Data.SQLite.EF6"
