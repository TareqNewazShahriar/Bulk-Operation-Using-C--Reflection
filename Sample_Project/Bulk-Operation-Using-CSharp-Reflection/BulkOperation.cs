using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

public class BulkOperation<T>
{
    #region Fields

    private List<PropertyInfo> properties = null;

	Action<T> _forEach;
	Action<List<T>> _atEnd;
    
    #endregion Fields

    #region Constructors

    public BulkOperation(List<T> list, Action<T> ForEach, Action<List<T>> AtEnd)
	{
		this._forEach = ForEach;
		this._atEnd = AtEnd;

        InitPropObject(typeof(T));
    }

    #endregion Constructors

    #region Public Methods
	
    #endregion Public Methods

    #region Private Methdos

    private void InitPropObject(Type _type)
	{
		// take only native and public proeperties
		properties = _type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList();
    }

    private List<object> GetValues(T obj)
    {
        object val = null;
		var values = new List<object>();
        // get values from the model
        foreach (var s in properties)
        {
            try
            {
                val = s.GetValue(obj) ?? DBNull.Value;
            }
            catch
            {
                val = DBNull.Value;
            }

			values.Add(val);
        }

		return values;
    }
	
    #endregion Private Methods
}
