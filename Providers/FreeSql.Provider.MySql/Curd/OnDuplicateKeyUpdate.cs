﻿using FreeSql.Aop;
using FreeSql.Internal;
using FreeSql.Internal.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FreeSql.MySql.Curd
{
    public class OnDuplicateKeyUpdate<T1> where T1 : class
    {
        internal MySqlInsert<T1> _mysqlInsert;
        internal MySqlUpdate<T1> _mysqlUpdatePriv;
        internal MySqlUpdate<T1> _mysqlUpdate => _mysqlUpdatePriv ?? (_mysqlUpdatePriv = new MySqlUpdate<T1>(_mysqlInsert.InternalOrm, _mysqlInsert.InternalCommonUtils, _mysqlInsert.InternalCommonExpression, null).NoneParameter().SetSource(_mysqlInsert.InternalSource) as MySqlUpdate<T1>);

        public OnDuplicateKeyUpdate(IInsert<T1> insert)
        {
            _mysqlInsert = insert as MySqlInsert<T1>;
            if (_mysqlInsert == null) throw new Exception("OnDuplicateKeyUpdate 是 FreeSql.Provider.MySql/FreeSql.Provider.MySqlConnector 特有的功能");
        }

        protected void ClearData()
        {
            _mysqlInsert.InternalClearData();
            _mysqlUpdatePriv = null;
        }

        public OnDuplicateKeyUpdate<T1> IgnoreColumns(Expression<Func<T1, object>> columns)
        {
            _mysqlUpdate.IgnoreColumns(columns);
            return this;
        }
        public OnDuplicateKeyUpdate<T1> UpdateColumns(Expression<Func<T1, object>> columns)
        {
            _mysqlUpdate.UpdateColumns(columns);
            return this;
        }
        public OnDuplicateKeyUpdate<T1> IgnoreColumns(string[] columns)
        {
            _mysqlUpdate.IgnoreColumns(columns);
            return this;
        }
        public OnDuplicateKeyUpdate<T1> UpdateColumns(string[] columns)
        {
            _mysqlUpdate.UpdateColumns(columns);
            return this;
        }

        public OnDuplicateKeyUpdate<T1> Set<TMember>(Expression<Func<T1, TMember>> column, TMember value)
        {
            _mysqlUpdate.Set(column, value);
            return this;
        }
        public OnDuplicateKeyUpdate<T1> Set<TMember>(Expression<Func<T1, TMember>> exp)
        {
            _mysqlUpdate.Set(exp);
            return this;
        }
        public OnDuplicateKeyUpdate<T1> SetRaw(string sql)
        {
            _mysqlUpdate.SetRaw(sql);
            return this;
        }

        public string ToSql()
        {
            var sb = new StringBuilder();
            sb.Append(_mysqlInsert.ToSql()).Append("\r\nON DUPLICATE KEY UPDATE\r\n");

            var sbSetEmpty = _mysqlUpdate.InternalSbSet.Length == 0;
            var sbSetIncrEmpty = _mysqlUpdate.InternalSbSetIncr.Length == 0;
            if (sbSetEmpty == false || sbSetIncrEmpty == false)
            {
                if (sbSetEmpty == false) sb.Append(_mysqlUpdate.InternalSbSet.ToString().Substring(2));
                if (sbSetIncrEmpty == false) sb.Append(sbSetEmpty ? _mysqlUpdate.InternalSbSetIncr.ToString().Substring(2) : _mysqlUpdate.InternalSbSetIncr.ToString());
            }
            else
            {
                var colidx = 0;
                foreach (var col in _mysqlInsert.InternalTable.Columns.Values)
                {
                    if (col.Attribute.IsPrimary || _mysqlUpdate.InternalIgnore.ContainsKey(col.Attribute.Name)) continue;

                    if (colidx > 0) sb.Append(", \r\n");

                    if (col.Attribute.IsVersion == true)
                    {
                        var field = _mysqlInsert.InternalCommonUtils.QuoteSqlName(col.Attribute.Name);
                        sb.Append(field).Append(" = ").Append(field).Append(" + 1");
                    }
                    else if (_mysqlInsert.InternalIgnore.ContainsKey(col.Attribute.Name))
                        sb.Append(_mysqlUpdate.InternalWhereCaseSource(col.CsName, sqlval => sqlval).Trim());
                    else
                    {
                        var field = _mysqlInsert.InternalCommonUtils.QuoteSqlName(col.Attribute.Name);
                        sb.Append(field).Append(" = VALUES(").Append(field).Append(")");
                    }
                    ++colidx;
                }
            }

            return sb.ToString();
        }

        public long ExecuteAffrows()
        {
            var sql = this.ToSql();
            if (string.IsNullOrEmpty(sql)) return 0;

            var before = new CurdBeforeEventArgs(_mysqlInsert.InternalTable.Type, _mysqlInsert.InternalTable, CurdType.Insert, sql, _mysqlInsert.InternalParams);
            _mysqlInsert.InternalOrm.Aop.CurdBefore?.Invoke(_mysqlInsert, before);
            long ret = 0;
            Exception exception = null;
            try
            {
                ret = _mysqlInsert.InternalOrm.Ado.ExecuteNonQuery(_mysqlInsert.InternalConnection, _mysqlInsert.InternalTransaction, CommandType.Text, sql, _mysqlInsert.InternalParams);
            }
            catch (Exception ex)
            {
                exception = ex;
                throw ex;
            }
            finally
            {
                var after = new CurdAfterEventArgs(before, exception, ret);
                _mysqlInsert.InternalOrm.Aop.CurdAfter?.Invoke(_mysqlInsert, after);
                ClearData();
            }
            return ret;
        }

#if net40
#else
        async public Task<long> ExecuteAffrowsAsync()
        {
            var sql = this.ToSql();
            if (string.IsNullOrEmpty(sql)) return 0;

            var before = new CurdBeforeEventArgs(_mysqlInsert.InternalTable.Type, _mysqlInsert.InternalTable, CurdType.Insert, sql, _mysqlInsert.InternalParams);
            _mysqlInsert.InternalOrm.Aop.CurdBefore?.Invoke(_mysqlInsert, before);
            long ret = 0;
            Exception exception = null;
            try
            {
                ret = await _mysqlInsert.InternalOrm.Ado.ExecuteNonQueryAsync(_mysqlInsert.InternalConnection, _mysqlInsert.InternalTransaction, CommandType.Text, sql, _mysqlInsert.InternalParams);
            }
            catch (Exception ex)
            {
                exception = ex;
                throw ex;
            }
            finally
            {
                var after = new CurdAfterEventArgs(before, exception, ret);
                _mysqlInsert.InternalOrm.Aop.CurdAfter?.Invoke(_mysqlInsert, after);
                ClearData();
            }
            return ret;
        }
#endif
    }
}