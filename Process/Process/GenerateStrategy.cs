using System;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

namespace DADStormProcess {

	public abstract class GenerateStrategy{
		public abstract object generateTuple(IList<string> finalTuple);

		//methods only used in CSF
		public virtual void reportBack () {}
		public virtual void reset ()      {}
	}

	/// <summary>
	/// Class that all CSF related class in inherit from, it will contain the tuple structure
	/// </summary>
	public abstract class CSF_TupleStructure  : GenerateStrategy {
		//Setup
		protected const int sourceIpIndex = 0;
		protected const int sourcePortIndex = 1;
		protected const int destinIpIndex = 2;
		protected const int destinPortIndex = 3;
		protected const int sizeOfPacketIndex = 4;
		protected const int protocolIndex = 5;
//		protected const int Index = 10;

		public abstract void processTuple(IList<string> tuple);

        protected virtual IList<IList<string>> defaultReturn (IList<string> l) {
			List<IList<string>> result = new List<IList<string>> ();
			result.Add (l);
			return result;
		}

		// always returns the tuple it receives
		public override object generateTuple (IList<string> tuple) {
			Task.Run(() => this.processTuple(tuple) );//TemplateMethod
			return defaultReturn (tuple);
		}
	}

	public class CustomDll : GenerateStrategy {
		private Assembly assembly;
		private string   methodName;
		private Type     type;
		private object   obj;

		public CustomDll(string dllName, string className, string methodName){
            System.Console.WriteLine("Setup\r\ndllName   : " + dllName + "\r\nclassName : " + className + "\r\nmethodName: " + methodName + "\r\n");
            this.assembly = Assembly.Load(File.ReadAllBytes(dllName));
			this.type = assembly.GetType (className);
			this.methodName = methodName;
			this.obj = Activator.CreateInstance (type);
		}

		public override object generateTuple(IList<string> finalTuple) {
			Object[] methodArgs = { finalTuple };
			return type.InvokeMember (methodName, BindingFlags.Default | BindingFlags.InvokeMethod, null, obj, methodArgs);
		}
	}
}
