/*
 * Base class for all SQL factories
 * Copyright (C) 2004 Morten Mertner
 * 
 * This library is free software; you can redistribute it and/or modify it 
 * under the terms of the GNU Lesser General Public License 2.1 or later, as
 * published by the Free Software Foundation. See the included License.txt
 * or http://www.gnu.org/copyleft/lesser.html for details.
 *
 * $Id: GentleSqlFactory.cs 1232 2008-03-14 05:36:00Z mm $
 */

using System;
using System.Data;
using Gentle.Common;

namespace Gentle.Framework
{

	#region Capability Enum
	[Flags]
	public enum Capability
	{
		/// <summary>
		/// This capability signifies that the database backend supports executing 
		/// multiple SQL statements in one batch. When this capability is not present
		/// Gentle will perform two queries for insert operations on tables with 
		/// autogenerated primary keys (one to perform the insert and one for 
		/// retrieving the generated key).
		/// </summary>
		BatchQuery = 1,
		/// <summary>
		/// This capability signifies that the database backend supports paging of
		/// result sets, i.e. that SQL can be generated to retrieve only a certain
		/// set of rows.
		/// </summary>
		Paging = 2,
		/// <summary>
		/// This capability signifies that the data provider supports named parameters.
		/// If not supported, positional parameters will be used instead.
		/// </summary>
		NamedParameters = 4
	}
	#endregion

	/// <summary>
	/// <p>The base class for all SQL factory implementations. Default implementations are provided
	/// for some of the methods.</p>
	/// <p>Inherit from this class when adding support for a new RDBMS.</p>
	/// </summary>
	public abstract class GentleSqlFactory
	{
		public long NO_DBTYPE = -1;

		/// <summary>
		/// The provider for which this factory is generating SQL.
		/// </summary>
		protected IGentleProvider provider;

		/// <summary>
		/// Construct a new GentleSqlFactory instance.
		/// </summary>
		protected GentleSqlFactory( IGentleProvider provider )
		{
			this.provider = provider;
		}

		/// <summary>
		/// Shortcut method for obtaining an IDbCommand instance.
		/// </summary>
		/// <returns></returns>
		public virtual IDbCommand GetCommand()
		{
			return provider.GetCommand();
		}

		/// <summary>
		/// Obtain the integer value of the database type corresponding to the given system type.
		/// The value returned must be castable to a valid type (enum value) for the current 
		/// persistence engine. This method is called to translate property types to database
		/// types when one has not been explicitly defined by the user or read from the database.
		/// </summary>
		/// <param name="type">The system type</param>
		/// <returns>The corresponding database type</returns>
		public abstract long GetDbType( Type type );

		/// <summary>
		/// This method converts the given string (as extracted from the database system tables) 
		/// to the corresponding type enumeration value. 
		/// </summary>
		/// <param name="dbType">The name of the type with the database engine used.</param>
		/// <param name="isUnsigned">A boolean value indicating whether the type is unsigned. This
		/// is not supported by most engines and/or data providers and is thus fairly useless at
		/// this point.</param>
		/// <returns>The value of the corresponding database type enumeration. The enum is converted
		/// to its numeric (long) representation because each provider uses its own enum (and they
		/// are not compatible with the generic DbType defined in System.Data).</returns>
		public abstract long GetDbType( string dbType, bool isUnsigned );

		/// <summary>
		/// This method should return the system type corresponding to the database specific type
		/// indicated by the long. The default implementation will throw an exception.
		/// </summary>
		/// <param name="dbType">The provider specific database type enum value.</param>
		/// <returns>The closest matching system type.</returns>
		public virtual Type GetSystemType( long dbType )
		{
			Check.Fail( Error.NotImplemented, "This method must be implemented in subclasses." );
			return null;
		}

		/// <summary>
		/// Obtain the default NullValue to use with the TableColumn attribute for columns mapping
		/// to a system type that does not allow null assignment. This method is intended for use
		/// by code generators.
		/// </summary>
		public object GetDefaultNullValue( Type type )
		{
			if( type == null || type == typeof(DateTime) )
			{
				return null;
			}
			switch( type.Name )
			{
				case "System.Byte":
				case "System.Int16":
				case "System.Int32":
				case "System.Int64":
				case "System.UInt16":
				case "System.UInt32":
				case "System.UInt64":
				case "System.Decimal":
				case "System.Enum":
					return 0;
				case "System.Single":
					return (float) 0.0;
				case "System.Double":
					return 0.0;
				case "System.DateTime":
					return DateTime.MinValue;
			}
			return null;
		}

		/// <summary>
		/// Formats the given table name for use in queries. This may include prefixing
		/// it with a schema name or suffixing it with an alias (for multi-table selects).
		/// This default implementation simply returns the string given.
		/// </summary>
		/// <param name="tableName">The table name to format</param>
		/// <returns>The formatted table name</returns>
		public virtual string GetTableName( string tableName )
		{
			return IsReservedWord( tableName ) ? QuoteReservedWord( tableName ) : tableName;
		}

		/// <summary>
		/// Returns the minimum supported DateTime value. This is a generic method
		/// returning the arbitrarily chosen value of 1/1/1800.
		/// </summary>
		public virtual DateTime MinimumSupportedDateTime
		{
			get { return new DateTime( 1800, 1, 1 ); }
		}

		/// <summary>
		/// Returns the maximum supported DateTime value. This is a generic method
		/// returning the arbitrarily chosen value of 1/1/3000.
		/// </summary>
		public virtual DateTime MaximumSupportedDateTime
		{
			get { return new DateTime( 3000, 1, 1 ); }
		}

		/// <summary>
		/// Obtain the character or string used to prefix parameters.
		/// </summary>
		public abstract string GetParameterPrefix();

		/// <summary>
		/// Obtain the character or string used to suffix parameters.
		/// </summary>
		public virtual string GetParameterSuffix()
		{
			return string.Empty;
		}

		/// <summary>
		/// Obtain the name to use for name-based indexing into the IDbCommand.Parameters
		/// collections. Most databases omit the parameter prefix, whereas some require it
		/// to be present (e.g. SQLite).
		/// </summary>
		/// <param name="paramName">The parameter name without quoting or prefix/suffix.</param>
		/// <returns>The name to use when accessing the IDbCommand.Parameters hashtable.</returns>
		public virtual string GetParameterCollectionKey( string paramName )
		{
			return paramName;
		}

		/// <summary>
		/// Obtain the character or string used to terminate or delimit statements.
		/// </summary>
		public virtual string GetStatementTerminator()
		{
			return ";";
		}

		/// <summary>
		/// Determine is a word is reserved and needs special quoting.
		/// </summary>
		/// <returns>True if the word is reserved</returns>
		public virtual bool IsReservedWord( string word )
		{
			return false;
		}

		/// <summary>
		/// Obtain a quoted version of the reserved word to allow the reserved word to be 
		/// used in queries anyway. If a reserved word cannot be quoted this method should
		/// raise an error informing the user that they need to pick a different name.
		/// </summary>
		/// <returns>The given reserved word or field quoted to avoid errors.</returns>
		public virtual string QuoteReservedWord( string word )
		{
			return word;
		}

		/// <summary>
		/// Obtain an enum describing the supported database capabilities. The default is
		/// to support all capabilities. See <see cref="Capability"/> for details on the 
		/// available capabilities.
		/// </summary>
		public virtual Capability Capabilities
		{
			get { return Capability.BatchQuery | Capability.Paging | Capability.NamedParameters; }
		}

		/// <summary>
		/// Call this method to check for the availability of a certain capability or
		/// combined set of capabilities (which must all be present).
		/// </summary>
		/// <param name="dc">The capability to check for.</param>
		/// <returns>True if the database supports the capability.</returns>
		public virtual bool HasCapability( Capability dc )
		{
			return (Capabilities & dc) != 0;
		}

		/// <summary>
		/// Produce the actual SQL string for the specified <see cref="Operator"/>. This is the part
		/// of the SQL string between the column name and the parameter value.
		/// </summary>
		/// <param name="op">The operator to convert to SQL</param>
		/// <param name="isValueNull">This parameter indicates whether the value of the parameter is null. This
		/// is required because different operators must be used for null equality checls.</param>
		/// <returns>The SQL string for the specified operator</returns>
		public virtual string GetOperatorBegin( Operator op, bool isValueNull )
		{
			switch( op )
			{
				case Operator.Equals:
					return isValueNull ? "is" : "=";
				case Operator.NotEquals:
					if( isValueNull )
					{
						return "is not";
					}
					else // TODO move checks like these to the GentleSqlFactory classes
					{
						return provider.Name == "Jet" ? "<>" : "!=";
					}
				case Operator.GreaterThan:
					return ">";
				case Operator.GreaterThanOrEquals:
					return ">=";
				case Operator.LessThan:
					return "<";
				case Operator.LessThanOrEquals:
					return "<=";
				case Operator.Like:
					return "like";
				case Operator.NotLike:
					return "not like";
				case Operator.In:
					return "in (";
				case Operator.NotIn:
					return "not in (";
				default:
					Check.Fail( Error.InvalidRequest, "Unable to format query string for unknown operator." );
					return null;
			}
		}

		/// <summary>
		/// Produce the actual SQL string for the specified <see cref="Operator"/>. This is the part
		/// of the SQL string after the parameter value. This string is usually empty.
		/// </summary>
		/// <param name="op">The operator to convert to SQL</param>
		/// <returns>The SQL string for the specified operator</returns>
		public virtual string GetOperatorEnd( Operator op )
		{
			if( op == Operator.In || op == Operator.NotIn )
			{
				return ")";
			}
			else
			{
				return "";
			}
		}

		/// <summary>
		/// Obtain the character used to delimit string parameters.
		/// </summary>
		/// <returns>The quote character.</returns>
		public abstract char GetQuoteCharacter();

		/// <summary>
		/// Get the statement for retrieving last inserted row id for auto-generated id columns
		/// </summary>
		/// <param name="sql"></param>
		/// <param name="om">An <see cref="Gentle.Framework.ObjectMap"/> instance of the object for which to retrieve the identity select</param>
		/// <returns></returns>
		public abstract string GetIdentitySelect( string sql, ObjectMap om );

		/// <summary>
		/// Add an SQL parameter to the given IDbCommand object.
		/// </summary>
		/// <param name="cmd">The IDbCommand object to operate on.</param>
		/// <param name="name">The name of the parameter.</param>
		/// <param name="type">The system type of the paramater.</param>
		// public abstract void AddParameter( IDbCommand cmd, string name, Type type );
		public virtual void AddParameter( IDbCommand cmd, string name, Type type )
		{
			AddParameter( cmd, name, GetDbType( type ) );
		}

		/// <summary>
		/// Add an SQL parameter to the given IDbCommand object.
		/// </summary>
		/// <param name="cmd">The IDbCommand object to operate on.</param>
		/// <param name="name">The name of the parameter.</param>
		/// <param name="dbType">The long value of the provider specific database type enum (e.g. DbType)</param>
		public abstract void AddParameter( IDbCommand cmd, string name, long dbType );
	}
}