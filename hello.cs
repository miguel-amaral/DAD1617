// Hello1.cs

using System;

public class Hello
{
	public string[] retornaInt(string[] args){
		Console.ForegroundColor = ConsoleColor.Red;
		System.Console.WriteLine("retorna int invoked with success\r\n");
		Console.ResetColor();
		string[] retorno = new string[1];
		retorno[0] = args.Length.ToString();	
		return retorno ;
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
