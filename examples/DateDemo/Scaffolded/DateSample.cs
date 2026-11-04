using System;
using System.Collections.Generic;
using IngresDates;

namespace DateDemo.Scaffolded;
public partial class DateSample
{
    public IngresDate LeadTime { get; set; }
    public IngresDate? NullableValue { get; set; }
    public int Id { get; set; }
    public IngresDate NativeValue { get; set; }
    public string Label { get; set; } = null !;
    public IngresDate RequiredDate { get; set; }
}
