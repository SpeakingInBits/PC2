// Each test gets its own browser context, so pages can be scanned in parallel
[assembly: Parallelize(Workers = 4, Scope = ExecutionScope.MethodLevel)]
