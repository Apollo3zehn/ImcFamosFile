## Texts

There are more types to fully describe a dataset. First, there is a [named text](xref:ImcFamosFile.FamosFileText):

```cs
var group = new FamosFileGroup("Generator");
group.Texts.Add(new FamosFileText("Type", "E-126"));
```

A named text can also contain more than one message:

```cs
var text = new FamosFileText("Random list of texts.", new List<string>() { "Text 1.", "Text 2?", "Text 3!" });

group.Texts.Add(text);
```

## Single Values

Another type is the [single value](xref:ImcFamosFile.FamosFileSingleValue):

```cs
group.SingleValues.Add(new FamosFileSingleValue<double>("GEN_TEMP_1_AVG", 40.25)
{
    Comment = "Generator temperature 1.",
    Unit = "°C",
    Time = DateTime.Now,
});
```

You can access the data of a single value by casting it to the correct type:

```cs
var singleValue = famosFile.SingleValues.First();
var bytes = singleValue.RawData // only bytes available

var typedSingleValue = (FamosFileSingleValue<float>)singleValue;
var floatData = typedSingleValue.Data; // now we get data with correct type
```

## Comments

As you can see, a single values takes a `Unit`, a `Comment` and a `Time`. You can add comments to many types like `groups`, `texts` and `channels`.

## Properties

Like comments, [properties](xref:ImcFamosFile.FamosFileProperty) can be added to several types (`single values`, `groups`, `texts` and `channels`):

```cs
channel.PropertyInfo = new FamosFilePropertyInfo(new List<FamosFileProperty>()
{
    new FamosFileProperty("Sensor Location", "Below generator.", FamosFilePropertyType.String),
    new FamosFileProperty("Sensor Creation Date", "07/15", FamosFilePropertyType.String)
});
```

Although you can specify the property type, it is stored as string within the file. However, you cannot store a string like "abc" as `FamosFilePropertyType.Integer`, so the data type still has a meaning.

## Custom keys

[Custom keys](xref:ImcFamosFile.FamosFileCustomKey) are a way to add additional binary information to the file:

```cs
var customKey = new FamosFileCustomKey("FileID", encoding.GetBytes(Guid.NewGuid().ToString()));

famosFile.CustomKeys.Add(customKey);
```

