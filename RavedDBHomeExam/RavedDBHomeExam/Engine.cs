using System;
using System.Collections.Generic;

namespace RavedDBHomeExam
{
    public class Engine
    {
        private static Dictionary<string, dynamic> listDict = new();//string key from the query, dynamic value for the matching list 
        private static Data data = new();
        public static string[] SplitQuery(string query)
        {//splitting the query according to the query form 
            string[] seperator = { "from", "FROM", "where", "WHERE", "select", "SELECT" };
            string[] parts = query.Split(seperator, 3, StringSplitOptions.RemoveEmptyEntries);
            string from, where = null, select;
            if (parts.Length == 3)
            {
                from = parts[0];
                where = parts[1];
                select = parts[2];
            }
            else if ((query.Contains("from") || query.Contains("FROM")) &&
                    (query.Contains("select") || query.Contains("SELECT")))
            {
                from = parts[0];
                select = parts[1];
            }
            else
                return null;
            string[] ret = { from, where, select };
            return ret;
        }


        private static Tuple<int, string> countAmountOfConditions(string where)
        {//counting the amount of conditions and formatting them
            int splitTo = 1;
            while (where.Contains("and") || where.Contains("AND"))
            {
                if (where.Contains("and"))
                    where = where.Replace("and", "&&");
                else
                    where = where.Replace("AND", "&&");
                splitTo++;
            }
            while (where.Contains("or") || where.Contains("OR"))
            {
                if (where.Contains("or"))
                    where = where.Replace("or", "||");
                else
                    where = where.Replace("OR", "||");
                splitTo++;
            }
            return Tuple.Create(splitTo, where);
        }

        private static string removeParenthesis(string condition)
        {//removing the parenthesis to make the condition "cleaner"
            if (condition.Contains('('))
                condition = condition.Replace('(', ' ');
            if (condition.Contains(')'))
                condition = condition.Replace(')', ' ');
            condition = condition.Trim();
            return condition;
        }

        private static Tuple<string, string> checkIfEquals(string condition, dynamic inst, string where)
        {//checking if the current instace fits the condtion(equals)
            if (!condition.Contains('>') && !condition.Contains('<'))//equals
            {
                string[] conParts = condition.Split('=');
                conParts[0] = conParts[0].Trim();//the field
                conParts[1] = conParts[1].Trim();//the value
                if (inst.GetType().GetProperty(conParts[0]).GetValue(inst, null).Equals(conParts[1]))
                {
                    where = where.Replace(condition, "true");
                    condition = "true";
                }
            }
            return Tuple.Create(where, condition);
        }

        private static Tuple<string, string> checkIfGreater(string condition, dynamic inst, string where)
        {//checking if the current instace fits the condtion(greater or equals)
            if (condition.Contains('>'))
            {
                if (condition.Contains("="))
                {
                    string[] conParts = condition.Split(">=");
                    conParts[0] = conParts[0].Trim();
                    if ((int)inst.GetType().GetProperty(conParts[0]).GetValue(inst, null) >= Int32.Parse(conParts[1]))
                    {
                        where = where.Replace(condition, "true");
                        condition = "true";
                    }
                }
                else
                {
                    string[] conParts = condition.Split('>');
                    conParts[0] = conParts[0].Trim();
                    if ((int)inst.GetType().GetProperty(conParts[0]).GetValue(inst, null) > Int32.Parse(conParts[1]))
                    {
                        where = where.Replace(condition, "true");
                        condition = "true";
                    }
                }
            }
            return Tuple.Create(where, condition);
        }

        private static Tuple<string, string> checkIfSmaller(string condition, dynamic inst, string where)
        {//checking if the current instace fits the condtion(smaller or equals)
            if (condition.Contains('<'))
            {
                if (condition.Contains('='))
                {
                    string[] conParts = condition.Split("<=");
                    conParts[0] = conParts[0].Trim();
                    if ((int)inst.GetType().GetProperty(conParts[0]).GetValue(inst, null) <= Int32.Parse(conParts[1]))
                    {
                        where = where.Replace(condition, "true");
                        condition = "true";
                    }
                }
                else
                {
                    string[] conParts = condition.Split('<');
                    conParts[0] = conParts[0].Trim();
                    if ((int)inst.GetType().GetProperty(conParts[0]).GetValue(inst, null) < Int32.Parse(conParts[1]))
                    {
                        where = where.Replace(condition, "true");
                        condition = "true";
                    }
                }
            }
            return Tuple.Create(where, condition);
        }

        private static string[] Select(string select, dynamic inst)
        {//returning the wanted fields from the query
            string[] selectRet;
            int count = 1;
            for (int i = 0; i < select.Length; i++)
                if (select[i] == ',')
                    count++;
            selectRet = select.Split(",", count, StringSplitOptions.RemoveEmptyEntries);
            string[] ret = new string[count];
            for (int i = 0; i < count; i++)
            {
                selectRet[i] = selectRet[i].Trim();
                ret[i] = inst.GetType().GetProperty(selectRet[i]).GetValue(inst, null).ToString();
            }
            return ret;
        }
        public static string[] MakeQuery(string query)
        {
            string[] queryParts = SplitQuery(query);
            string from = queryParts[0], where = queryParts[1], select = queryParts[2];
            if (queryParts == null)
                throw FormatException();

            if (where != null)
            {
                var tup = countAmountOfConditions(where);
                int splitTo = tup.Item1;//amount of condtions
                where = tup.Item2;//the "format" condtions
                string[] splitBy = { "||", "&&" };
                from = from.Trim();
                foreach (var inst in listDict[from])
                {
                    string[] conditions = where.Split(splitBy, splitTo, StringSplitOptions.RemoveEmptyEntries);
                    string whereTemp = where;
                    for (int i = 0; i < conditions.Length; i++)
                    {
                        conditions[i] = removeParenthesis(conditions[i]);
                        var tuple = checkIfEquals(conditions[i], inst, whereTemp);
                        whereTemp = tuple.Item1;
                        conditions[i] = tuple.Item2;

                        tuple = checkIfGreater(conditions[i], inst, whereTemp);
                        whereTemp = tuple.Item1;
                        conditions[i] = tuple.Item2;

                        tuple = checkIfSmaller(conditions[i], inst, whereTemp);
                        whereTemp = tuple.Item1;
                        conditions[i] = tuple.Item2;

                        if (conditions[i] != "true")
                        {
                            whereTemp = whereTemp.Replace(conditions[i], "false");
                            conditions[i] = "false";
                        }
                    }
                    if (StringToBool(whereTemp))//checking if the total result of all the conditions is true
                        return Select(select, inst);
                }
            }
            return null;
        }

        private static bool StringToBool(string condition)
        {//reducing the condition to true/false
            string[] splits;
            if (condition.Contains("&&"))
            {
                splits = condition.Split("&&");
                return StringToBool(splits[0]) && StringToBool(splits[1]);
            }
            if (condition.Contains("||"))
            {
                splits = condition.Split("||");
                return StringToBool(splits[0]) || StringToBool(splits[1]);
            }
            if (condition.Contains("true"))
                return true;

            return false;
        }



        private static Exception FormatException()
        {
            throw new NotImplementedException("The querry must have 'from' and 'select' fields");
        }

        public static void fillDictionary()
        {
            addListToDict("Users", data.Users);
            addListToDict("Order", data.Order);
        }
        public static void addListToDict(string name, dynamic list)
        {
            listDict.Add(name, list);
        }
        static void Main(string[] args)
        {

            data.Users = new();
            fillDictionary();
            data.Users.Add(new User("aaa@aa.a", "John Doe", 35));
            data.Users.Add(new User("aa@a.c", "foo", 30));
            string[] res = MakeQuery("from Users where FullName = John Doe AND Age > 30 select Email");
            foreach (string str in res)
                Console.WriteLine(str);
            res = MakeQuery("from Users where(FullName = foo or FullName = bar) and Age <= 90 select FullName, Email, Age");
            foreach (string str in res)
                Console.WriteLine(str);
        }
    }
}
