﻿// From http://flee.codeplex.com/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using NUnit.Framework;

namespace Expressions.Test.FleeLanguage
{
    public abstract class ExpressionTests
    {
        private const char SEPARATOR_CHAR = ';';
        protected delegate void LineProcessor(string[] lineParts);

        protected ExpressionOwner MyValidExpressionsOwner = new ExpressionOwner();
        protected ExpressionContext MyGenericContext;
        protected ExpressionContext MyValidCastsContext;

        protected ExpressionContext MyCurrentContext;

        protected static readonly CultureInfo TestCulture = CultureInfo.GetCultureInfo("en-CA");

        public ExpressionTests()
        {
            MyValidExpressionsOwner = new ExpressionOwner();

            MyGenericContext = this.CreateGenericContext(MyValidExpressionsOwner);

            var imports = new[]
            {
                new Import(typeof(Mouse)),
                new Import(typeof(Monitor)),
                new Import(typeof(Convert), "Convert"),
                new Import(typeof(Guid)),
                new Import(typeof(Version)),
                new Import(typeof(DayOfWeek)),
                new Import(typeof(DayOfWeek), "DayOfWeek"),
                new Import(typeof(ValueType)),
                new Import(typeof(IComparable)),
                new Import(typeof(ICloneable)),
                new Import(typeof(Array)),
                new Import(typeof(System.Delegate)),
                new Import(typeof(AppDomainInitializer)),
                new Import(typeof(System.Text.Encoding)),
                new Import(typeof(System.Text.ASCIIEncoding)),
                new Import(typeof(ArgumentException)),
                // ImportBuiltinTypes
                new Import(typeof(bool), "boolean"),
		        new Import(typeof(byte), "byte"),
		        new Import(typeof(sbyte), "sbyte"),
		        new Import(typeof(short), "short"),
		        new Import(typeof(ushort), "ushort"),
		        new Import(typeof(int), "int"),
		        new Import(typeof(uint), "uint"),
		        new Import(typeof(long), "long"),
		        new Import(typeof(ulong), "ulong"),
		        new Import(typeof(float), "single"),
		        new Import(typeof(double), "double"),
		        new Import(typeof(decimal), "decimal"),
		        new Import(typeof(char), "char"),
		        new Import(typeof(object), "object"),
		        new Import(typeof(string), "string")
           };

           ExpressionContext context = new ExpressionContext(imports, MyValidExpressionsOwner);

            //context.Options.OwnerMemberAccess = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;

            //context.Imports.ImportBuiltinTypes();

            MyValidCastsContext = context;

            // For testing virtual properties
            //TypeDescriptor.AddProvider(new UselessTypeDescriptionProvider(TypeDescriptor.GetProvider(typeof(int))), typeof(int));
            //TypeDescriptor.AddProvider(new UselessTypeDescriptionProvider(TypeDescriptor.GetProvider(typeof(string))), typeof(string));

            Initialize();
        }


        protected virtual void Initialize()
        {
        }

        protected ExpressionContext CreateGenericContext(object owner)
        {
            var imports = new[]
            {
                new Import(typeof(Math), "Math"),
                new Import(typeof(Uri), "Uri"),
                new Import(typeof(DateTime), "DateTime"),
                new Import(typeof(Convert), "Convert"),
                new Import(typeof(Type), "Type"),
                new Import(typeof(DayOfWeek), "DayOfWeek"),
                new Import(typeof(ConsoleModifiers), "ConsoleModifiers"),
                // ImportBuiltinTypes
                new Import(typeof(bool), "boolean"),
		        new Import(typeof(byte), "byte"),
		        new Import(typeof(sbyte), "sbyte"),
		        new Import(typeof(short), "short"),
		        new Import(typeof(ushort), "ushort"),
		        new Import(typeof(int), "int"),
		        new Import(typeof(uint), "uint"),
		        new Import(typeof(long), "long"),
		        new Import(typeof(ulong), "ulong"),
		        new Import(typeof(float), "single"),
		        new Import(typeof(double), "double"),
		        new Import(typeof(decimal), "decimal"),
		        new Import(typeof(char), "char"),
		        new Import(typeof(object), "object"),
		        new Import(typeof(string), "string")
            };

            var context = new ExpressionContext(imports, owner);

            //context.Options.OwnerMemberAccess = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            //context.Imports.ImportBuiltinTypes();


            //NamespaceImport ns1 = new NamespaceImport("ns1");
            //NamespaceImport ns2 = new NamespaceImport("ns2");

            //ns2.Add(new TypeImport(typeof(Math)));

            //ns1.Add(ns2);

            //context.Imports.RootImport.Add(ns1);

            context.Variables.Add("varInt32", 100);
            context.Variables.Add("varDecimal", new decimal(100));
            context.Variables.Add("varString", "string");

            return context;
        }

        protected void AssertCompileException(string expression)
        {
            try
            {
                new DynamicExpression(expression, ExpressionLanguage.Flee);
                Assert.Fail();

            }
            catch
            {
            }
        }

        protected void AssertCompileException(string expression, ExpressionContext context)
        {
            try
            {
                new DynamicExpression(expression, ExpressionLanguage.Flee).Bind(context);
                Assert.Fail("Compile exception expected");
            }
            catch
            {
                //Assert.AreEqual(expectedReason, ex.Reason, string.Format("Expected reason '{0}' but got '{1}'", expectedReason, ex.Reason));
            }
        }

        protected void DoTest(DynamicExpression expression, ExpressionContext expressionContext, string result, Type resultType, CultureInfo testCulture)
        {
            if (ReferenceEquals(resultType, typeof(object)))
            {
                Type expectedType = Type.GetType(result, false, true);

                if (expectedType == null)
                {
                    // Try to get the type from the Tests assembly
                    result = string.Format("{0}.{1}", this.GetType().Namespace, result);
                    expectedType = this.GetType().Assembly.GetType(result, true, true);
                }

                object expressionResult = expression.Invoke(expressionContext);

                if (object.ReferenceEquals(expectedType, typeof(void)))
                {
                    Assert.IsNull(expressionResult);
                }
                else
                {
                    Assert.That(expressionResult, Is.InstanceOf(expectedType));
                }

            }
            else
            {
                TypeConverter tc = TypeDescriptor.GetConverter(resultType);

                object expectedResult = tc.ConvertFromString(null, testCulture, result);
                object actualResult = expression.Invoke(expressionContext);

                expectedResult = RoundIfReal(expectedResult);
                actualResult = RoundIfReal(actualResult);

                Assert.AreEqual(expectedResult, actualResult);
            }
        }

        protected object RoundIfReal(object value)
        {
            if (object.ReferenceEquals(value.GetType(), typeof(double)))
            {
                double d = (double)value;
                d = Math.Round(d, 4);
                return d;
            }
            else if (object.ReferenceEquals(value.GetType(), typeof(float)))
            {
                float s = (float)value;
                s = (float)Math.Round(s, 4);
                return s;
            }
            else
            {
                return value;
            }
        }

        protected void ProcessScriptTests(string scriptFileName, LineProcessor processor)
        {
            this.WriteMessage("Testing: {0}", scriptFileName);

            System.IO.Stream instream = this.GetScriptFile(scriptFileName);
            System.IO.StreamReader sr = new System.IO.StreamReader(instream);

            try
            {
                this.ProcessLines(sr, processor);
            }
            finally
            {
                sr.Close();
            }
        }

        private void ProcessLines(System.IO.TextReader sr, LineProcessor processor)
        {
            while (sr.Peek() != -1)
            {
                string line = sr.ReadLine();
                this.ProcessLine(line, processor);
            }
        }

        private void ProcessLine(string line, LineProcessor processor)
        {
            if (line.StartsWith("'") == true)
            {
                return;
            }

            try
            {
                string[] arr = line.Split(SEPARATOR_CHAR);
                processor(arr);
            }
            catch
            {
                this.WriteMessage("Failed line: {0}", line);
                throw;
            }
        }

        protected System.IO.Stream GetScriptFile(string fileName)
        {
            Assembly a = Assembly.GetExecutingAssembly();
            return a.GetManifestResourceStream(GetType().Namespace + ".TestScripts." + fileName);
        }

        protected string GetIndividualTest(string testName)
        {
            throw new NotImplementedException();

            //Assembly a = Assembly.GetExecutingAssembly();

            //Stream s = a.GetManifestResourceStream(this.GetType(), "IndividualTests.xml");

            //XPathDocument doc = new XPathDocument(s);

            //XPathNavigator nav = doc.CreateNavigator();

            //nav = nav.SelectSingleNode(string.Format("Tests/Test[@Name='{0}']", testName));

            //string str = (string)nav.TypedValue;

            //s.Close();

            //return str;
        }

        protected void WriteMessage(string msg, params object[] args)
        {
            msg = string.Format(msg, args);
            Console.WriteLine(msg);
        }

        protected static object Parse(string s)
        {
            bool b = false;

            if (bool.TryParse(s, out b) == true)
            {
                return b;
            }

            int i = 0;

            if (int.TryParse(s, NumberStyles.Integer, TestCulture, out i) == true)
            {
                return i;
            }

            double d = 0;

            if (double.TryParse(s, NumberStyles.Float, TestCulture, out d) == true)
            {
                return d;
            }

            DateTime dt = default(DateTime);

            if (DateTime.TryParse(s, TestCulture, DateTimeStyles.None, out dt) == true)
            {
                return dt;
            }

            return s;
        }

        protected static IDictionary<string, object> ParseQueryString(string s)
        {
            string[] arr = s.Split('&');
            Dictionary<string, object> dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (string part in arr)
            {
                string[] arr2 = part.Split('=');
                dict.Add(arr2[0], Parse(arr2[1]));
            }

            return dict;
        }
    }
}
