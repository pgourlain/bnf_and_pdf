using System.Collections;
using PdfSharpDslCore.Parser;

namespace pdfsharpdslTests
{
    public class VariablesDictionaryTests
    {
        [Fact]
        public void DictionarySupportsMutationAndSystemVariables()
        {
            var requestedSystemVariables = new List<string>();
            var variables = new VariablesDictionary(name =>
            {
                requestedSystemVariables.Add(name);
                return name == "PAGEWIDTH" ? 595 : 842;
            });

            variables.Add("FIRST", 1);
            variables.Add(new KeyValuePair<string, object?>("SECOND", 2));

            Assert.False(variables.IsReadOnly);
            Assert.Equal(2, variables.Count);
            Assert.Contains("FIRST", variables.Keys);
            Assert.Contains(2, variables.Values);
            Assert.True(variables.ContainsKey("SECOND"));
            Assert.Equal(1, variables["FIRST"]);
            Assert.Null(variables["MISSING"]);
            Assert.Equal(595, variables["PAGEWIDTH"]);
            Assert.Equal(842, variables["PAGEHEIGHT"]);
            Assert.Equal(new[] { "PAGEWIDTH", "PAGEHEIGHT" }, requestedSystemVariables);
            Assert.Throws<NotImplementedException>(() => variables["FIRST"] = 3);

            Assert.True(variables.Remove("FIRST"));
            Assert.False(variables.Remove("MISSING"));
            variables.Clear();
            Assert.Empty(variables.Keys);
        }

        [Fact]
        public void SaveAndRestorePreserveOuterScope()
        {
            var variables = new VariablesDictionary(_ => 0);
            variables.Add("VALUE", "outer");

            variables.SaveVariables();
            variables.Add("VALUE", "inner");
            variables.Add("INNER_ONLY", true);

            Assert.Equal("inner", variables["VALUE"]);
            variables.RestoreVariables();
            Assert.Equal("outer", variables["VALUE"]);
            Assert.False(variables.ContainsKey("INNER_ONLY"));
        }

        [Fact]
        public void GlobalScopeDisablesSaveAndRestore()
        {
            var variables = new VariablesDictionary(_ => 0)
            {
                GlobalScope = true
            };
            variables.Add("VALUE", "outer");

            variables.SaveVariables();
            variables.Add("VALUE", "global");
            variables.RestoreVariables();

            Assert.Equal("global", variables["VALUE"]);
        }

        [Fact]
        public void UnsupportedCollectionMembersThrow()
        {
            var variables = new VariablesDictionary(_ => 0);
            var item = new KeyValuePair<string, object?>("VALUE", 1);

            Assert.Throws<NotImplementedException>(() => variables.Contains(item));
            Assert.Throws<NotImplementedException>(() => variables.CopyTo(new[] { item }, 0));
            Assert.Throws<NotImplementedException>(() => variables.Remove(item));
            Assert.Throws<NotImplementedException>(() => variables.GetEnumerator());
            Assert.Throws<NotImplementedException>(() => ((IEnumerable)variables).GetEnumerator());
        }
    }
}