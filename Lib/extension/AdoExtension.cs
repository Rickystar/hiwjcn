﻿using Dapper;
using Lib.core;
using Lib.helper;
using MySql.Data.Common;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Polly;
using Lib.data;

namespace Lib.extension
{
    /// <summary>
    /// 对dapper的扩展
    /// </summary>
    public static class DapperExtension
    {
        /// <summary>
        /// 如果没有打开链接就打开链接
        /// </summary>
        public static void OpenIfClosedWithRetry(this IDbConnection con, int retryCount = 1)
        {
            if (con.State == ConnectionState.Closed)
            {
                new Action(() => con.Open()).InvokeWithRetryAndWait<Exception>(retryCount, i => TimeSpan.FromMilliseconds(i * 100));
            }
        }

        /// <summary>
        /// 开启事务
        /// </summary>
        /// <param name="con"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static IDbTransaction StartTransaction(this IDbConnection con, IsolationLevel? level)
        {
            if (level == null)
            {
                return con.BeginTransaction();
            }
            else
            {
                return con.BeginTransaction(level.Value);
            }
        }

        /// <summary>
        /// HowToUseParametersInDapperFramework
        /// </summary>
        /// <param name="con"></param>
        public static void HowToUseParametersInDapperFramework(this IDbConnection con)
        {
            var args = new DynamicParameters(new { });
            args.Add("age", 1);
            //con.Execute("", args);
            throw new Exception($"这个方法只做记录，不可以调用，请使用{nameof(ParseToDapperParameters)}");
        }

        /// <summary>
        /// 把dbparameter转换为dapper的参数（测试可用）
        /// </summary>
        /// <param name="con"></param>
        /// <param name="ps"></param>
        /// <returns></returns>
        public static DynamicParameters ParseToDapperParameters(this IDbConnection con, IEnumerable<DbParameter> ps)
        {
            var args = new DynamicParameters(new { });
            foreach (var p in ps)
            {
                args.Add(p.ParameterName, p.Value);
            }
            return args;
        }

        /// <summary>
        /// 获取表结构
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        [Obsolete("还没写完")]
        public static (string table_name, Dictionary<string, string> keys, Dictionary<string, string> columns)
            GetTableStructure<T>(this T model) where T : IDBTable
        {
            var t = model.GetType();
            var pps = t.GetProperties().Where(x => x.CanRead).Where(x => !x.GetCustomAttributes<NotMappedAttribute>().Any());
            //table
            var table_name = t.GetCustomAttribute<TableAttribute>()?.Name ?? t.Name;

            //读取字段名和sql的placeholder
            (string column, string placeholder) GetColumn(PropertyInfo p)
            {
                var column = p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name;
                var placeholder = p.Name;
                return (column, placeholder);
            }

            //keys
            var key_props = pps.Where(x => x.GetCustomAttributes<KeyAttribute>().Any()).ToList();
            var keys = key_props.Select(x => GetColumn(x)).ToDictionary(x => x.column, x => x.placeholder);

            //columns
            var column_props = pps.Where(x =>
            !(x.GetCustomAttributes<KeyAttribute>().Any() || x.GetCustomAttributes<DatabaseGeneratedAttribute>().Any())).ToList();
            if (!ValidateHelper.IsPlumpList(column_props)) { throw new Exception("无法提取到有效字段"); }
            var columns = column_props.Select(x => GetColumn(x)).ToDictionary(x => x.column, x => x.placeholder);
            /*
             var props = new Dictionary<string, string>();
            foreach (var p in t.GetProperties().Where(x => x.CanRead))
            {
                //跳过不映射字段
                if (p.GetCustomAttributes<NotMappedAttribute>().Any()) { continue; }
                //跳过自增
                if (p.GetCustomAttributes<DatabaseGeneratedAttribute>().Any()) { continue; }
                //获取字段
                var column = p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name;

                props[column] = p.Name;
            }
             */
            return (table_name, keys, columns);
        }

        /// <summary>
        /// 获取插入SQL
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static string GetInsertSql<T>(this T model) where T : IDBTable
        {
            var t = model.GetType();
            var table_name = t.GetCustomAttribute<TableAttribute>()?.Name ?? t.Name;

            var props = new Dictionary<string, string>();
            foreach (var p in t.GetProperties().Where(x => x.CanRead))
            {
                //跳过不映射字段
                if (p.GetCustomAttributes<NotMappedAttribute>().Any()) { continue; }
                //跳过自增
                if (p.GetCustomAttributes<DatabaseGeneratedAttribute>().Any()) { continue; }
                //获取字段
                var column = p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name;

                props[column] = p.Name;
            }
            if (!ValidateHelper.IsPlumpDict(props)) { throw new Exception("无法提取到有效字段"); }

            var k = string.Join(",", props.Keys);
            var v = string.Join(",", props.Values.Select(x => "@" + x));
            var sql = $"INSERT INTO {table_name} ({k}) VALUES ({v})";
            return sql;
        }

        /// <summary>
        /// 插入数据（使用System.ComponentModel.DataAnnotations.Schema配置数据表映射）
        /// </summary>
        /// <param name="con"></param>
        /// <param name="model"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public static int Insert<T>(this IDbConnection con, T model,
            IDbTransaction transaction = null, int? commandTimeout = default(int?)) where T : IDBTable
        {
            var sql = model.GetInsertSql();
            try
            {
                return con.Execute(sql, model, transaction: transaction, commandTimeout: commandTimeout);
            }
            catch (Exception e)
            {
                throw new Exception($"无法执行SQL:{sql}", e);
            }
        }

        /// <summary>
        /// 异步插入
        /// </summary>
        /// <param name="con"></param>
        /// <param name="model"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public static async Task<int> InsertAsync<T>(this IDbConnection con, T model,
            IDbTransaction transaction = null, int? commandTimeout = default(int?)) where T : IDBTable
        {
            var sql = model.GetInsertSql();
            try
            {
                return await con.ExecuteAsync(sql, model, transaction: transaction, commandTimeout: commandTimeout);
            }
            catch (Exception e)
            {
                throw new Exception($"无法执行SQL:{sql}", e);
            }
        }

        /// <summary>
        /// 获取更新sql
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static string GetUpdateSql<T>(this T model) where T : IDBTable
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// mysql ado扩展
    /// </summary>
    public static class MysqlAdoExtension
    {
        /// <summary>
        /// 获取带timeout的链接字符串
        /// </summary>
        /// <param name="con"></param>
        /// <returns></returns>
        public static string GetTimeoutConnectionString(this MySqlConnection con)
        {
            var b = new MySqlConnectionStringBuilder(ConfigHelper.Instance.MySqlConnectionString);
            b.ConnectionTimeout = 3;
            var str = b.ToString();
            return str;
        }
    }

    /// <summary>
    /// 对Ado的扩展
    /// </summary>
    public static class AdoExtension
    {
        /// <summary>
        /// 这个方法来自途虎养车网，自己做了一些小修改
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        [Obsolete("强烈推荐使用Dapper")]
        public static T CurrentModel<T>(this IDataReader reader) where T : class, new()
        {
            return MapperHelper.GetModelFromReader<T>(reader);
        }

        /// <summary>
        /// 读取list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        [Obsolete("强烈推荐使用Dapper")]
        public static List<T> WholeList<T>(this IDataReader reader) where T : class, new()
        {
            return MapperHelper.GetListFromReader<T>(reader);
        }

        /*
         JArray array = new JArray();
        array.Add("Manual text");
        array.Add(new DateTime(2000, 5, 23));

        JObject o = new JObject();
        o["MyArray"] = array;

        string json = o.ToString();
        // {
        //   "MyArray": [
        //     "Manual text",
        //     "2000-05-23T00:00:00"
        //   ]
        // }
         */

        /// <summary>
        /// 获取Json（测试可用）
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        [Obsolete("强烈推荐使用Dapper")]
        public static string GetJson(this IDataReader reader)
        {
            var fields = new List<string>();
            for (int i = 0; i < reader.FieldCount; ++i)
            {
                fields.Add(reader.GetName(i));
            }

            var arr = new JArray();
            while (reader.Read())
            {
                var jo = new JObject();
                fields.ForEach(x =>
                {
                    var val = JToken.FromObject(reader[x]);
                    jo[x] = val;
                });

                arr.Add(jo);
            }
            return arr.ToString();
        }

        /// <summary>
        /// 多种方式绑定参数
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="parameters"></param>
        [Obsolete("强烈推荐使用Dapper")]
        public static void AddParamsSmartly(this IDbCommand cmd, params object[] parameters)
        {
            if (!ValidateHelper.IsPlumpList(parameters)) { return; }
            object first_p = null;
            if (parameters.Length == 1 && !((first_p = parameters[0]) is IDataParameter))
            {
                string dbType = null;
                var connectionType = cmd.Connection.GetType();
                if (connectionType == typeof(SqlConnection))
                {
                    dbType = "sqlserver";
                }
                if (connectionType == typeof(MySqlConnection))
                {
                    dbType = "mysql";
                }
                if (dbType == null)
                {
                    throw new Exception("不支持的数据库类型");
                }
                var list = new List<object>();
                var props = first_p.GetType().GetProperties();
                foreach (var p in props)
                {
                    var name = p.Name;
                    object val = p.GetValue(first_p);
                    if (val == null) { val = DBNull.Value; }

                    if (dbType == "sqlserver")
                    {
                        list.Add(new SqlParameter("@" + name, val));
                    }
                    if (dbType == "mysql")
                    {
                        list.Add(new MySqlParameter("@" + name, val));
                    }
                }
                list.ForEach(x => cmd.Parameters.Add(x));
            }
            else
            {
                parameters.ToList().ForEach(x => cmd.Parameters.Add(x));
            }
        }

        /// <summary>
        /// AddParameters
        /// </summary>
        /// <param name="command"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        [Obsolete("添加参数")]
        public static void AddParameters(this IDbCommand command, string key, object value)
        {
            var p = command.CreateParameter();
            p.ParameterName = key;
            p.Value = value ?? System.DBNull.Value;
            command.Parameters.Add(p);
        }

        /// <summary>
        /// 执行SQL
        /// </summary>
        /// <param name="con"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        [Obsolete("强烈推荐使用Dapper")]
        public static int ExecuteSql(this IDbConnection con, string sql, params object[] parameters)
        {
            var count = 0;
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sql;
                cmd.AddParamsSmartly(parameters);
                count = cmd.ExecuteNonQuery();
            }
            return count;
        }

        /// <summary>
        /// 执行存储过程
        /// </summary>
        /// <param name="con"></param>
        /// <param name="procedure_name"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        [Obsolete("强烈推荐使用Dapper")]
        public static int ExecuteProcedure(this IDbConnection con, string procedure_name, params object[] parameters)
        {
            var count = 0;
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = procedure_name;
                cmd.AddParamsSmartly(parameters);
                count = cmd.ExecuteNonQuery();
            }
            return count;
        }

        /// <summary>
        /// 读取第一行第一个记录
        /// </summary>
        /// <param name="con"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        [Obsolete("强烈推荐使用Dapper")]
        public static object ExecuteScalar(this IDbConnection con, string sql, params object[] parameters)
        {
            object obj = null;
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sql;
                cmd.AddParamsSmartly(parameters);
                obj = cmd.ExecuteScalar();
            }
            return obj;
        }


        /// <summary>
        /// 用reader读取list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="con"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        [Obsolete("强烈推荐使用Dapper")]
        public static List<T> GetList<T>(this IDbConnection con, string sql, params object[] parameters) where T : class, new()
        {
            List<T> list = null;
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sql;
                cmd.AddParamsSmartly(parameters);
                using (var reader = cmd.ExecuteReader())
                {
                    list = reader.WholeList<T>();
                }
            }
            return list;
        }

        /// <summary>
        /// 读取reader的json格式（测试可用）
        /// </summary>
        /// <param name="con"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        [Obsolete("强烈推荐使用Dapper")]
        public static string GetJson(this IDbConnection con, string sql, params object[] parameters)
        {
            var json = string.Empty;
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sql;
                cmd.AddParamsSmartly(parameters);
                using (var reader = cmd.ExecuteReader())
                {
                    json = reader.GetJson();
                }
            }
            return json;
        }

        /// <summary>
        /// 读取reader第一条
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="con"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        [Obsolete("强烈推荐使用Dapper")]
        public static T GetFirst<T>(this IDbConnection con, string sql, params object[] parameters) where T : class, new()
        {
            T model = default(T);
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sql;
                cmd.AddParamsSmartly(parameters);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader != null && reader.Read())
                    {
                        model = reader.CurrentModel<T>();
                    }
                }
            }
            return model;
        }

    }
}
