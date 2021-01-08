//css_args -l:0
using System.IO;
using System;
using System.Linq;
using static dbg; // print() extension

if(args.Any())
	print(File.ReadAllText(args[0]));

