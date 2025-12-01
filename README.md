# LyuEModbus



## Modbus 的四种数据类型

| 类型 | 比喻 | 数据 | 读/写 |
|------|------|------|-------|
| **线圈 (Coil)** | 开关 | true/false | 读写 |
| **离散输入** | 传感器开关 | true/false | 只读 |
| **保持寄存器** | 可调数值 | 0-65535 | 读写 |
| **输入寄存器** | 传感器数值 | 0-65535 | 只读 |

**注意**：线圈和寄存器的地址是独立的！

- 线圈地址 0 和 保持寄存器地址 0 是两个不同的数据

  

## 功能码

| 功能码 | 操作 |
|-------|------|
| 01 | 读线圈 |
| 03 | 读保持寄存器 |
| 05 | 写单个线圈 |
| 06 | 写单个寄存器 |
| 15 | 写多个线圈 |
| 16 | 写多个寄存器 |



## 快速开始（目前仅TCP/IP）

### 安装

LyuEModbus

---

## ModbusTcpMaster（主站）

### 完整配置示例（链式）

```csharp
var master = ModbusTcpMaster.Create()
    .WithAddress("127.0.0.1", 502)
    .WithSlaveId(1) //多从站时添加
    .WithTimeout(3000, 3000)
    .WithAutoReconnect(3000, 10)          // 自动重连：间隔3秒，最多10次
    .WithHeartbeat(3000)                   // 心跳检测：每3秒检测一次,重载可添加心跳Func事件
    .WithLog(msg => Console.WriteLine(msg)) // 使用Ilogger打印
    .WithConnectionChanged(connected => 
    {
        Console.WriteLine(connected ? "已连接" : "已断开");//connected为bool
    })
    .WithReconnecting(attempt => //attempt 重连第attempt次事件
    {
        Console.WriteLine($"重连中... ({attempt})");
    });

await master.ConnectAsync();
```

### 基础读取操作

```csharp
// 读取保持寄存器，从地址0开始读取10个，返回 ushort[]
ushort[] registers = await master.ReadHoldingRegistersAsync(0, 10);

// 读取线圈，从地址0开始读取10个，返回 bool[]
bool[] coils = await master.ReadCoilsAsync(0, 10);

// 读取输入寄存器，从地址0开始读取10个，返回 ushort[]
ushort[] inputs = await master.ReadInputRegistersAsync(0, 10);

// 读取离散输入，从地址0开始读取10个，返回 bool[]
bool[] discreteInputs = await master.ReadInputsAsync(0, 10);
```

### 基础写入操作

```csharp
// 写入单个寄存器，地址0写入值100
await master.WriteSingleRegisterAsync(0, 100);

// 写入多个寄存器，从地址0开始写入数组
await master.WriteMultipleRegistersAsync(0, new ushort[] { 100, 200, 300 });

// 写入单个线圈，地址0写入true
await master.WriteSingleCoilAsync(0, true);

// 写入多个线圈，从地址0开始写入数组
await master.WriteMultipleCoilsAsync(0, new bool[] { true, false, true });
```

### 扩展读取方法（带默认错误回调Func/Action：null，失败返回null）

```csharp
// 读取单个线圈，返回 bool?
bool? coil = await master.ReadCoilAsync(0);

// 读取多个线圈返回字典，返回 Dictionary<ushort, bool>?
Dictionary<ushort, bool>? coilDict = await master.ReadCoilsToDictAsync(0, 10);

// 读取单个保持寄存器，返回 ushort?
ushort? reg = await master.ReadHoldingRegisterAsync(0);

// 读取多个保持寄存器返回字典，返回 Dictionary<ushort, ushort>?
Dictionary<ushort, ushort>? regDict = await master.ReadHoldingRegistersToDictAsync(0, 10);

// 读取单个输入寄存器，返回 ushort?
ushort? inputReg = await master.ReadInputRegisterAsync(0);

// 读取多个输入寄存器返回字典，返回 Dictionary<ushort, ushort>?
Dictionary<ushort, ushort>? inputDict = await master.ReadInputRegistersToDictAsync(0, 10);

// 读取单个离散输入，返回 bool?
bool? input = await master.ReadInputAsync(0);

// 读取多个离散输入返回字典，返回 Dictionary<ushort, bool>?
Dictionary<ushort, bool>? inputBoolDict = await master.ReadInputsToDictAsync(0, 10);

// 读取32位整数（两个连续寄存器），bigEndian指定大小端，返回 int?
int? intValue = await master.ReadInt32Async(0, bigEndian: true);

// 读取32位浮点数（两个连续寄存器），返回 float?
float? floatValue = await master.ReadFloatAsync(0, bigEndian: true);
```

### 扩展写入方法（带默认错误回调Func/Action：null，失败返回false）

```csharp
// 写入单个线圈，返回 bool 表示是否成功
bool success1 = await master.WriteCoilAsync(0, true);

// 写入多个线圈，返回 bool
bool success2 = await master.WriteCoilsAsync(0, new bool[] { true, false });

// 批量写入线圈（字典方式），返回 bool
bool success3 = await master.WriteCoilsAsync(new Dictionary<ushort, bool> { { 0, true }, { 1, false } });

// 写入单个寄存器，返回 bool
bool success4 = await master.WriteRegisterAsync(0, 100);

// 写入多个寄存器，返回 bool
bool success5 = await master.WriteRegistersAsync(0, new ushort[] { 100, 200 });

// 批量写入寄存器（字典方式），返回 bool
bool success6 = await master.WriteRegistersAsync(new Dictionary<ushort, ushort> { { 0, 100 }, { 1, 200 } });

// 写入32位整数（两个连续寄存器），返回 bool
bool success7 = await master.WriteInt32Async(0, 12345, bigEndian: true);

// 写入32位浮点数（两个连续寄存器），返回 bool
bool success8 = await master.WriteFloatAsync(0, 3.14f, bigEndian: true);
```

### 特殊操作扩展

```csharp
// 切换线圈状态（读取当前值并取反写入），返回新状态 bool?
bool? newState = await master.ToggleCoilAsync(0);

// 寄存器值增加，返回新值 ushort?
ushort? newValue = await master.IncrementRegisterAsync(0, increment: 1);

// 寄存器值减少，返回新值 ushort?
ushort? newValue2 = await master.DecrementRegisterAsync(0, decrement: 1);
```

### 断开连接

```csharp
master.Disconnect();
master.Dispose();
```

---

## ModbusTcpSlave（从站）

### 完整配置示例

```csharp
var slave = ModbusTcpSlave.Create()
    .WithAddress("0.0.0.0", 502)
    .WithSlaveId(1)
    .WithInitHoldingRegisters(100)
    .WithInitCoils(100)
    .WithLog(msg => Console.WriteLine(msg))
    .WithStatusChanged(running => 
    {
        Console.WriteLine(running ? "从站已启动" : "从站已停止");
    })
    .WithHoldingRegisterWritten((address, oldValue, newValue) =>
    {
        Console.WriteLine($"寄存器[{address}]: {oldValue} → {newValue}");
    })
    .WithCoilWritten((address, value) =>
    {
        Console.WriteLine($"线圈[{address}]: {value}");
    })
    .WithClientConnected(client =>
    {
        Console.WriteLine($"客户端已连接: {client}");
    })
    .WithClientDisconnected(client =>
    {
        Console.WriteLine($"客户端已断开: {client}");
    });

await slave.StartAsync();
```

### 读写从站数据

```csharp
// 设置保持寄存器值 (address: 寄存器地址, value: 写入值)
slave.SetHoldingRegister(address: 0, value: 100);

// 批量设置保持寄存器 (startAddress: 起始地址, values: 写入数据数组)
slave.SetHoldingRegisters(startAddress: 0, values: new ushort[] { 100, 200, 300 });

// 设置线圈值 (address: 线圈地址, value: 写入值)
slave.SetCoil(address: 0, value: true);

// 读取保持寄存器 (startAddress: 起始地址, count: 读取数量)
ushort[]? values = slave.ReadHoldingRegisters(startAddress: 0, count: 10);
```

### 停止从站

```csharp
slave.Stop();
slave.Dispose();
```

---

## License

MIT
