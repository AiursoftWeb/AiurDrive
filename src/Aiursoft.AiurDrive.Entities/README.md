# Aiursoft Entity Framework Core Entity Modeling Specification

## Core Philosophy: Dictator Mode

1.  **DbSet Dictatorship**: Data creation, update, and deletion (CUD) must be performed through `DbContext.DbSet<T>`.
2.  **Explicit Loading**: Implicit behaviors (such as lazy loading) are prohibited; data must be explicitly `Include`d.
3.  **Compile-Time Contract**: Leverage C# type system (Type System) to expose potential errors at compile time, rather than deferring them to runtime.
4.  **Table Visibility**: All entity classes must clearly express their data columns and relationships, avoiding implicit conventions. All entity classes must appear in `DbContext` as `public DbSet<Repo> Repos => Set<Repo>();`, and table names must be the plural form of the entity class name (e.g., `User` corresponds to `Users` table).

-----

## 1. Primary Key Specification

**1.1 Type Restrictions**
The primary key of all entity classes must be of type `int` or `Guid`.

  * **Exception**: Entity classes that directly inherit from `Microsoft.AspNetCore.Identity.IdentityUser` are exempt (default uses `string`, allowed to remain unchanged).

**1.2 Mandatory Attribute**
The primary key of all entity classes must be decorated with the `[Key]` attribute.

**1.3 Immutability**
The primary key of all entity classes must be `{ get; init; }` (immutable after creation).

> **Example:**
>
> ```csharp
> [Key]
> public Guid Id { get; init; }
> ```

-----

## 2. Foreign Keys & Navigation Specification

**2.1 Pair Definition**
All foreign key relationships must be explicitly defined with two properties: the **foreign key ID** and the **navigation reference**.

**2.2 Foreign Key ID Nullability**

  * **Required Relationship**: The foreign key ID must be `Guid` (or `int`), and the property must be `required`.
  * **Optional Relationship**: The foreign key ID must be `Guid?` (or `int?`), and the property must be `required` (explicitly assign `null` when needed).

**2.3 Navigational References Must Be Nullable**
All **navigational reference properties** must be declared as **nullable types** (e.g., `User?`).

  * *Reason*: When creating a new `Entity()`, we typically only assign the ID without assigning the object. Declaring them as nullable satisfies C# compiler initialization checks and avoids false warnings.

**2.4 Navigational Properties Must Be NotNull**
All **navigational reference properties** must be annotated with `[NotNull]`.

**2.5 Serialization Masking**
All **navigational reference properties** must be annotated with `[Newtonsoft.Json.JsonIgnore]` (to prevent infinite loops).

**2.6 No Virtual (No Lazy Loading)**
All navigational properties **must not** use the `virtual` keyword.

  * *Consequence*: Completely disables Lazy Loading Proxy.
  * *Requirement*: When querying related data, explicitly call `.Include()` and `.ThenInclude()`.

> **Example:**
>
> ```csharp
> // Foreign key ID (required)
> public required Guid UserId { get; set; }
>
> // Navigational reference (must be nullable, must be NotNull, no virtual)
> [JsonIgnore]
> [ForeignKey(nameof(UserId))]
> [NotNull]
> public User? User { get; set; }
> ```

-----

## 3. Collections & Dictatorship

**3.1 Type Restrictions**
All reverse navigational collections must be declared as `IEnumerable<T>` type.

  * *Purpose*: Compile-time removal of `.Add()` and `.Remove()` methods from the collection.

**3.2 Initialization**
All collection properties must be initialized to `new List<T>()` to prevent null reference exceptions.

**3.3 Explicit Association**
All collection properties must be annotated with `[InverseProperty]`.

**3.4 Dictatorship Modification Principle**
Prohibit casting `IEnumerable` to `List` for data manipulation.

  * **Add child item**: A new object must be created and `_dbContext.Add(newItem)`.
  * **Remove child item**: The object must be queried and `_dbContext.Remove(item)`.

> **Example:**
>
> ```csharp
> [InverseProperty(nameof(ExamPaperSubmission.User))]
> public IEnumerable<ExamPaperSubmission> Submissions { get; init; } = new List<ExamPaperSubmission>();
> ```

-----

## 4. Data Column Standards (Data Columns)

**4.1 Length Constraints**
All columns of type `string` or `byte[]` must be constrained by `[MaxLength]`.

**4.2 Nullability Semantics**

  * **Non-nullable columns**: Use non-nullable types (e.g., `string`), and never use `[NotNull]` on nullable types.
  * **Nullable columns**: Use nullable types (e.g., `string?`), and must document in XML comments what business state **null represents**.

**4.3 Immutable Properties**
All properties that are not editable in business terms (e.g., `CreationTime`) must be `{ get; init; }`.

-----

## 5. Complete Standard AiurDrive (V3.0)

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis; // 尽量少用，除非用于辅助方法
using Newtonsoft.Json;

namespace Aiursoft.Exam.Entities;

public class AiurDriveEntity
{
    // [规则 1.1, 1.2, 1.3] 主键：Guid/int, Key, init
    [Key]
    public Guid Id { get; init; }

    // [规则 4.1, 4.2] 必填列：required string, MaxLength, 禁止 [NotNull]
    [MaxLength(100)]
    public required string Name { get; set; }

    // [规则 4.2, 4.3] 可空列：string?, 注释说明含义, init (如果是创建时不变量)
    /// <summary>
    /// 描述信息。
    /// 若为空，表示用户在创建时未填写备注。
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; init; }

    // [规则 4.3] 系统字段
    public DateTime CreationTime { get; init; } = DateTime.UtcNow;

    // ================= 关联关系 =================

    // [规则 2.2] 外键ID：required (即使是 Guid? 也要 required 以强制显式赋值)
    public required Guid ParentId { get; set; }

    // [规则 2.3, 2.4, 2.5, 2.6] 
    // 导航引用：Type?, JsonIgnore, ForeignKey, NotNull
    // 严禁 virtual (禁用延迟加载)
    [JsonIgnore]
    [ForeignKey(nameof(ParentId))]
    [NotNull]
    public ParentEntity? Parent { get; set; }

    // [规则 3.1, 3.2, 3.3] 
    // 集合：IEnumerable (独裁模式), InverseProperty, new List()
    // 严禁 virtual
    [InverseProperty(nameof(ChildEntity.Parent))]
    public IEnumerable<ChildEntity> Children { get; init; } = new List<ChildEntity>();
}
```