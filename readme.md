# Restme.Dapper (RESTme Dapper Wrapper) 

This is a set of Dapper .Net wrappers and helpers aims to simplify data manipulations in large scaled applications without compromise of using ORM.

### Features
* Implemented based on the latest .NET Core 1.0
* Simple methods and flexible calls

### Usage

#### 1. (Optional, but good practice) Apply Db Attributes to Entities
```csharp
    [RestmeTable("Users", DefaultOrderByClauseInQuery = "Id desc")]
    public class OEliteUser : IRestmeDbEntity
    {
        [RestmeDbColumn("UserName", RestmeDbColumnType.NormalColumn)]
        public string UserName { get; set; }

        [RestmeDbColumn("Id", RestmeDbColumnType.PrimaryKey)]
        public int Id { get; set; }

        [RestmeDbColumn]
        public bool IsActive { get; set; }
    }

    //class below is a useful collection wrapper, representing a set of OEliteUser when expecting multiple records to be returned
    public class OEliteUserCollection : List<OEliteUser>, IRestmeDbEntityCollection<OEliteUser>
    {
        //particuarly useful when the returned results are paginated - this will return the total count
        public int TotalRecordsCount {get;set;}
    }
```

* **[RestmeTable()]** => *RestmeTableAttribute* is used to to define an entity class mapping with a data table/view
* **[RestmeDbColumn()]** => *RestmeDbColumnAttribute* is used to map an entity property with a data table/view column
  - You can simply add [ResmeDbColumn] without any value, in which case the Property's name will be automatically mapped as the data table's column name.

#### 2. (Optional, but best practice) Define your DbContext
It'll be nice to have a DbContext similar like the Entity Framework does, although we certainly don't want same level of complexity which affects performance!

Our DbContext concept here is to provie a Schema structure of the database so when we do the entity-related queries, we can focus on the "primary" entity table, and expand the query accordingly. The RESTmd Dapper Wrapper will take care of the SQL query, compose them accordingly and then pass into the ADO.NET connection which then quickly returns the expected results.

An example of the DbContext can be like this:
```csharp
    public partial class DbCentre : RestmeDb
    {
        private const string _connectionString = "YOUR CONNECTION STRING";

        public DbCentre():base(_connectionString) {}

        //Representation of the Users table (and map with User class in your code)
        public OEliteUserQuery Users => DbQuery<OEliteUser, OEliteUserQuery>();

        //Representation of the Logins table (and map with UserLogin class in your code) 
        public UserLoginQuery UserLogins => DbQuery<UserLogin, UserLoginQuery>();
		
    }
```

NOTE, here I mentioned *OEliteUserQuery*, which is a class defined based on RestmeDbQuery, this is optional, however by inheriting from RestmeDbQuery it will allow you to customize the Db Execution queries such as custom select/insert/update/delete based on your particular needs

```csharp
public class OEliteUserDbQuery : RestmeDbQuery<OEliteUser>
{
    public OEliteUserDbQuery(RestmeDb dbCentre) : base(dbCentre)
    {
    }

    //example: we provided a custom table source for SELECT query, which instead of using the normal "Users" table, we query against a view called "vw_Users".
    public override string CustomSelectTableSource => "vw_Users";   

    //most other actions/methods are customizable by overriding them, enjoy 
}
```


#### 3. Query and CRUD from database
```csharp
//example 1.  passing bespoke parameters (most popular use case)
public async Task<OEliteUser> GetUserAsync(int userId)
{
    //Use a custom DbContext class (e.g. DbCentre) as below, or use RestmeDb(connStr)  directly if you didn't create custom DbContext as shown above
    using(var dbCentre = new DbCentre())  
    {
        //e.g. get an user by query against Users.Id column
        var user = await dbCentre.Users.Query("Id=@Id").Params(new {Id = userId}).FetchAsync<OSite>();
    }
}

//example 2. passing exsting object (Same type) which has multiple parameters preset and defined in custom DbQuery (such as OEliteUserQuery defined in example above)
public async Task<OEliteUser> GetUserAsync(OEliteUser user)
{
    using(var dbCentre = new DbCentre())  
    {
        var user = await dbCentre.Users.Query("Id=@Id").FetchAsync<OSite>(); 
    }
}

```
In example 2, RestmeDb will feed all found property values of object "user" to your query (here, only property "Id" will be used because there's only "@Id" parameter in the SQL query)

```csharp

//example 3. paginated set of search results
public async Task<List<OEliteUser>> FindUsersAsync(int pageIndex, int pageSize, string keyword, bool isActive)
{
    using (var dbCentre = new DbCentre())
    {
        var query = "username like '%@keyword%' and isActive = @isActive";

        result = await dbCentre.Users.Query(query)
                .Params(new {keyword = keyword, isActive = isActive})
                .Paginated(pageIndex, pageSize, "id desc")
                .FetchAsync<OEliteUser, OEliteUserCollection>();
    }
}
``` 

```csharp

//example 4. updated entire object
public async Task<bool> UpdateUserAsync(OEliteUser user)
{
    var affectedRowCount = await dbCentre.Users.Update(user, "Id=@Id").ExecuteAsync();
    return affectedRowCount > 0;
}
```
NOTE: there are more where clause control and parameters in the Update() method, enjoy.

```csharp

//example 5. delete record using entity object
public async Task<bool> DeleteUserAsync(OEliteUser user)
{
    var affectedRowCount = await dbCentre.Users.Delete(user, "Id=@Id").ExecuteAsync();
    return affectedRowCount > 0;
}

//example 6. delete record using parameters
public async Task<bool> DeleteUserAsync(int userId)
{
    var affectedRowCount = await dbCentre.Users.Delete("Id=@Id", new {Id = userId}).ExecuteAsync();
    return affectedRowCount > 0;
}
```

### Contributions

This is a simple library just recently created, your contribution is welcomed.

### License
Released under [MIT License](http://choosealicense.com/licenses/mit).
