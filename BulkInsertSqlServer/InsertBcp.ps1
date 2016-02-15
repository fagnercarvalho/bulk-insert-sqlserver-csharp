Function InitTests() 
{
    DeleteRecords

    for($i = 0; $i -le 499; $i++) 
    {
        InsertBcp
    }

    $average = Average($times.ToArray())

    "{0}: {1} ms" -f "InsertBcp", $average
    "TEST ENDED."
}

Function InsertBcp()
{
    $sw = [Diagnostics.Stopwatch]::StartNew()

    bcp TestTable in "InsertBcp.csv" -T -t -S "(localdb)\MSSQLLocalDB" -d BulkInsertSqlServer -f "FormatFile.xml"

    $sw.Stop()

    $times.Add($sw.Elapsed.TotalMilliseconds)

    DeleteRecords
}

Function Average($array)
{
    $total = 0;
    foreach($i in $array)
    {
        $total += $i
    }
    return ([decimal]($total) / [decimal]($array.Length));
}

Function DeleteRecords() 
{
    sqlcmd -S "(localdb)\MSSQLLocalDB" -d BulkInsertSqlServer -Q "TRUNCATE TABLE TestTable"
}

$times = New-Object System.Collections.Generic.List[System.Decimal]

InitTests