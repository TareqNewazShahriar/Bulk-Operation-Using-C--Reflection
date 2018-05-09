public class ModelProcssing<T> : BaseContext where T : class
{
    #region Fields

    //Dictionary<string, object> parameters = null;
    List<SqlParameter> parameters = null;
    List<PropertyInfo> properties = null;
    IEnumerable<string> propNames = null;
    IEnumerable<string> propsToUpdate = null;
    IEnumerable<string> colsForUnique = null;
    string primaryKeyColName = null;
    string sqlStatement = null;
    string globalTableName = null;

    #endregion Fields

    #region Constructors

    /// <summary>
    /// 
    /// </summary>
    /// <param name="autoPk">There are 3 Scenario for autoPK parameter:
    /// 1. PK col name ends with '_PK' and auto incremented then need to pass: true [bool]
    /// 2. Auto incremented primary col doesn't end with '_PK' then need to pass : "col_name" [string]
    /// 3. PK col name is not auto incremented then skip this parameter</param>
    /// <param name="_db">Pass the db entities object which generally instantiated on VMBase</param>
    public PriceUpdateModelProcssing(object autoPk = null, DBEntities _db = null)
        : base(_db ?? new DBEntities())
    {
        Type type = typeof(T);

        InitPropObject(type, autoPk);

        PrepareInsertSql(type.Name);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="colsForUnique">Column List to identify the specific record to update</param>
    /// <param name="autoPk">There are 3 Scenario for autoPK parameter:
    /// 1. PK col name ends with '_PK' and auto incremented then need to pass: true [bool]
    /// 2. Auto incremented primary col doesn't end with '_PK' then need to pass : "col_name" [string]
    /// 3. PK col name is not auto incremented then skip this parameter</param>
    /// <param name="_db">Pass the db entities object which generally instantiated on VMBase</param>
    public PriceUpdateModelProcssing(string[] colsForUnique, object autoPk = null, DBEntities _db = null)
        : base(_db ?? new DBEntities())
    {
        this.colsForUnique = colsForUnique;

        Type type = typeof(T);

        InitPropObject(type, autoPk);

        // if all the properties are needed to check record uniqueness then use only one property for fake update
        propsToUpdate = colsForUnique.Length >= propNames.Count() ? new List<string> { propNames.First() } : propNames.Except(colsForUnique);

        PrepareInsertOrUpdateSql(type.Name);
    }

    #endregion Constructors

    #region Public Methods

    public object Insert(T model)
    {
        GetValues(model);
        List<SqlParameter> paramList = CloneSqlParam();
        return ExecuteScalar(sqlStatement, CommandType.Text, paramList);
    }

    /// <summary>
    /// If record exists it will update, otherwise it will insert. This method will call reflection only first time and keep that into dictionary and later it will reuse from dictionary
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public object Merge(T model)
    {
        GetValues(model);

        List<SqlParameter> paramList = CloneSqlParam();
        return ExecuteScalar(sqlStatement, CommandType.Text, paramList);
    }

    private List<SqlParameter> CloneSqlParam()
    {
        List<SqlParameter> paramList = new List<SqlParameter>();

        foreach (SqlParameter par in parameters)
        {
            SqlParameter p = new SqlParameter();
            p.ParameterName = par.ParameterName;
            p.SqlDbType = par.SqlDbType;
            p.Size = par.Size;
            p.Value = par.Value;
            paramList.Add(p);
        }
        return paramList;
    }

    #endregion Public Methods

    #region Private Methdos

    private void InitPropObject(Type type, object autoPk)
    {
        parameters = new List<SqlParameter>();
        globalTableName = type.Name;

        parameters = GetParameters(globalTableName);
        var pkParameter = parameters.SingleOrDefault(x => x.ParameterName.ToUpper().EndsWith("_PK"));
        primaryKeyColName = pkParameter != null ? pkParameter.ParameterName.TrimStart('@') : "";

        // take only native and public proeperties
        properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList();

        //exclude auto increamented column
        if (autoPk != null)
        {
            string autoPkName = null;

            if (autoPk.GetType() == typeof(bool) && ((bool)autoPk))
                autoPkName = parameters.Single(x => x.ParameterName.EndsWith("_PK")).ParameterName.TrimStart('@');
            else if (autoPk.GetType() == typeof(string))
                autoPkName = autoPk.ToString();

            // remove auto pk column from properties, since this field cannot be inserted or updated
            parameters.RemoveAll(x => x.ParameterName == "@" + autoPkName);
        }

        // take all property names.
        propNames = parameters.Select(x => x.ParameterName.TrimStart('@'));

        properties = properties.Where(x => propNames.Contains(x.Name)).ToList();
    }

    private void GetValues(T model)
    {
        object val = null;
        // get values from the model
        foreach (var s in properties)
        {
            try
            {
                val = s.GetValue(model) ?? DBNull.Value;
            }
            catch
            {
                val = DBNull.Value;
            }

            parameters.Single(p => p.ParameterName == "@" + s.Name).Value = val;
        }
    }

    private void PrepareInsertSql(string className)
    {
        sqlStatement = string.Format(@" INSERT INTO {0} ({1}) VALUES ({2}); ",
            className, /*Param 0*/
            propNames.Aggregate((x, y) => x + ", " + y) /*Param 1 */,
            propNames.Aggregate
            (
                new StringBuilder(),
                (sb, item) => sb.AppendFormat("@{0}, ", item),
                sb => sb.Length >= 2 ? sb.ToString().Remove(sb.Length - 2) : ""
            ) /*param 2*/);
    }

    private void PrepareInsertOrUpdateSql(string className)
    {
        sqlStatement += string.Format
            (
                @" MERGE {0} AS target
                    USING (SELECT {1}) AS SOURCE({2}) 
                        ON({3})
                    WHEN matched THEN
                        UPDATE 
                        SET {4}
                    WHEN NOT matched BY target THEN
                        INSERT ({5})
                        VALUES ({6})
                    {7};",
                className, // Param 0
                colsForUnique.Aggregate
                (
                    new StringBuilder(),
                    (sb, item) => sb.Append("@" + item + ", "),
                    sb => sb.Length >= 2 ? sb.ToString().Remove(sb.Length - 2) : ""
                ), // param 1
                colsForUnique.Aggregate((x, y) => x + ", " + y), // Param 2
                colsForUnique.Aggregate
                (
                    new StringBuilder(),
                    (sb, item) => sb.AppendFormat("target.{0} = source.{0} AND ", item),
                    sb => sb.Length >= 4 ? sb.ToString().Remove(sb.Length - 4) : ""
                ), // Param 3
                propsToUpdate.Aggregate
                (
                    new StringBuilder(),
                    (sb, item) => sb.AppendFormat("{0} = @{0}, ", item),
                    sb => sb.Length >= 2 ? sb.ToString().Remove(sb.Length - 2) : ""
                ), // Param 4
                propNames.Aggregate((x, y) => x + ", " + y), // Param 5
                propNames.Aggregate
                (
                    new StringBuilder(),
                    (sb, item) => sb.Append("@" + item + ", "),
                    sb => sb.Length >= 2 ? sb.ToString().Remove(sb.Length - 2) : ""
                ),  // Param 6
                string.IsNullOrEmpty(primaryKeyColName) == false ? "OUTPUT Inserted." + primaryKeyColName : ""  // Param 7
            );


        //if ()
        //    sqlStatement += ;


        //sqlStatement += ";";
    }

    #endregion Private Methods
}
