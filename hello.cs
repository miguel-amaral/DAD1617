// Hello1.cs

using System;
using System.Collections.Generic;

public class Hello
{
	public IList<IList<string>> plusString(IList<string> old){
			int i = old.Count;
			string print = "from hello.dll method plusString:" + i;
			old.Add(i.ToString ());
			List<IList<string>> neW = new List<IList<string>>();
			neW.Add(old);
			System.Console.WriteLine(print);

			return neW;
	}
	public void Hello0(){
		System.Console.WriteLine("Hello, World!");
	}
	public void Hello1(string[] arg){
		System.Console.WriteLine( "Hello, World! arg1: " + arg[0] );
	}
	public void Hello2(string[] args){
		System.Console.WriteLine( "Hello, World! arg1: " + args[0] + " arg2: " + args[1] );
	}
	public static void Main(string[] args) {
		foreach (string str in args) {
			System.Console.WriteLine("Hello, World!: " + str);
		}
		System.Console.WriteLine("Hello, World!");
	}
}
