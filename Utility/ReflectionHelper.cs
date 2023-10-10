using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EngagedSkyblock.Utility {
	public class ReflectionHelper {
		private Type type;
		private object obj;
		public ReflectionHelper(object obj) {
			this.obj = obj;
			type = obj.GetType();
		}
		private SortedDictionary<string, FieldInfo> fieldInfos = new();
		private void CheckValidField(ref FieldInfo fieldInfo) {
			if (fieldInfo == null) {
				throw new ArgumentException($"Private field '{fieldInfo.Name}' not found on type '{type.FullName}'.");
			}
		}
		public T GetField<T>(string fieldName, BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance) {
			FieldInfo fieldInfo;
			if (!fieldInfos.TryGetValue(fieldName, out fieldInfo)) {
				fieldInfo = type.GetField(fieldName, bindingFlags);
			}

			CheckValidField(ref fieldInfo);

			return (T)fieldInfo.GetValue(obj);
		}
		public void SetField<T>(string fieldName, T value, BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance) {
			FieldInfo fieldInfo;
			if (!fieldInfos.TryGetValue(fieldName, out fieldInfo)) {
				fieldInfo = type.GetField(fieldName, bindingFlags);
			}

			CheckValidField(ref fieldInfo);

			fieldInfo.SetValue(obj, value);
		}
		public static T GetNonPublicStaticClassField<T>(string NameSpace, string className, string fieldName, BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Static) {
			Assembly assembly = Assembly.Load(NameSpace);
			Type backupIOType = assembly.GetType($"{NameSpace}.{className}");
			FieldInfo fieldInfo = backupIOType.GetField(fieldName, bindingFlags);
			return (T)fieldInfo.GetValue(null);
		}
		public static void SetNonPublicStaticClassField<T>(string NameSpace, string className, string fieldName, T value, BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Static) {
			Assembly assembly = Assembly.Load(NameSpace);
			Type backupIOType = assembly.GetType($"{NameSpace}.{className}");
			FieldInfo fieldInfo = backupIOType.GetField(fieldName, bindingFlags);
			fieldInfo.SetValue(null, value);
		}
	}
}
