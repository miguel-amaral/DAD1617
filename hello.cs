// Hello1.cs
public class Hello
{
	public void Hello0(){
		System.Console.WriteLine("Hello, World!");
	}
	public void Hello1(string arg1){
		System.Console.WriteLine( "Hello, World! arg1: " + arg1 );
	}
	public void Hello2(string arg1, string arg2){
		System.Console.WriteLine( "Hello, World! arg1: " + arg1 + " arg2: " + arg2 );
	}
	public static void Main(string[] args) {
		foreach (string str in args) {
			System.Console.WriteLine("Hello, World!: " + str);
		}
		System.Console.WriteLine("Hello, World!");
	}
}
