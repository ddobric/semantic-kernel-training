using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VibeNetAdapte
{
    using ModelContextProtocol.Server;
    using System;
    using System.ComponentModel;
    using System.Management.Automation; // Add reference to System.Management.Automation.dll

    [McpServerToolType]
    [Description("Provides methods to manage and retrieve information about network adapters, corresponding to PowerShell NetAdapter cmdlets.")]
    public class NetAdapterTool
    {
        public static PSDataCollection<PSObject> RunPowerShell(string script)
        {
            using (PowerShell ps = PowerShell.Create())
            {
                ps.AddScript("Import-Module NetAdapter; " + script);
                var results = ps.Invoke();
                if (ps.Streams.Error.Count > 0)
                    throw new Exception("PowerShell Error: " + ps.Streams.Error[0].ToString());
                return new PSDataCollection<PSObject>(results);
            }
        }

      

        [McpServerTool, Description("Adds a network adapter.")]
        public void AddNetAdapter(string name, string interfaceDescription = null)
        {
            string cmd = $"Add-NetAdapter -Name \"{name}\"";
            if (!string.IsNullOrWhiteSpace(interfaceDescription))
                cmd += $" -InterfaceDescription \"{interfaceDescription}\"";
            RunPowerShell(cmd);
        }

        [McpServerTool, Description("Disables a network adapter.")]
        public void DisableNetAdapter(string name)
        {
            RunPowerShell($"Disable-NetAdapter -Name \"{name}\" -Confirm:$false");
        }

        [McpServerTool, Description("Disables a binding for a network adapter.")]
        public void DisableNetAdapterBinding(string name, string componentId)
        {
            RunPowerShell($"Disable-NetAdapterBinding -Name \"{name}\" -ComponentID \"{componentId}\" -Confirm:$false");
        }

        [McpServerTool, Description("Disables the various checksum offload settings from network adapters that support these checksum offloads.")]
        public void DisableNetAdapterChecksumOffload(string name)
        {
            RunPowerShell($"Disable-NetAdapterChecksumOffload -Name \"{name}\"");
        }

        [McpServerTool, Description("Disables IPsec offload on a network adapter.")]
        public void DisableNetAdapterIPsecOffload(string name)
        {
            RunPowerShell($"Disable-NetAdapterIPsecOffload -Name \"{name}\"");
        }

        [McpServerTool, Description("Disables large send offload (LSO) on a network adapter.")]
        public void DisableNetAdapterLso(string name)
        {
            RunPowerShell($"Disable-NetAdapterLso -Name \"{name}\"");
        }

        [McpServerTool, Description("Disables packet direct on a network adapter.")]
        public void DisableNetAdapterPacketDirect(string name)
        {
            RunPowerShell($"Disable-NetAdapterPacketDirect -Name \"{name}\"");
        }

        [McpServerTool, Description("Disables power management on a network adapter.")]
        public void DisableNetAdapterPowerManagement(string name)
        {
            RunPowerShell($"Disable-NetAdapterPowerManagement -Name \"{name}\"");
        }

        [McpServerTool, Description("Disables QoS (Quality of Service) on a network adapter.")]
        public void DisableNetAdapterQos(string name)
        {
            RunPowerShell($"Disable-NetAdapterQos -Name \"{name}\"");
        }

        [McpServerTool, Description("Disables receive segment coalescing (RSC) on a network adapter.")]
        public void DisableNetAdapterRsc(string name)
        {
            RunPowerShell($"Disable-NetAdapterRsc -Name \"{name}\"");
        }

        [McpServerTool, Description("Disables receive side scaling (RSS) on a network adapter.")]
        public void DisableNetAdapterRss(string name)
        {
            RunPowerShell($"Disable-NetAdapterRss -Name \"{name}\"");
        }

        [McpServerTool, Description("Disables single root I/O virtualization (SR-IOV) on a network adapter.")]
        public void DisableNetAdapterSriov(string name)
        {
            RunPowerShell($"Disable-NetAdapterSriov -Name \"{name}\"");
        }

        [McpServerTool, Description("Disables virtual machine queue (VMQ) on a network adapter.")]
        public void DisableNetAdapterVmq(string name)
        {
            RunPowerShell($"Disable-NetAdapterVmq -Name \"{name}\"");
        }

        [McpServerTool, Description("Enables a network adapter.")]
        public void EnableNetAdapter(string name)
        {
            RunPowerShell($"Enable-NetAdapter -Name \"{name}\"");
        }

        [McpServerTool, Description("Enables a binding for a network adapter.")]
        public void EnableNetAdapterBinding(string name, string componentId)
        {
            RunPowerShell($"Enable-NetAdapterBinding -Name \"{name}\" -ComponentID \"{componentId}\"");
        }

        [McpServerTool, Description("Enables the various checksum offload settings from network adapters that support these checksum offloads.")]
        public void EnableNetAdapterChecksumOffload(string name)
        {
            RunPowerShell($"Enable-NetAdapterChecksumOffload -Name \"{name}\"");
        }

        [McpServerTool, Description("Enables IPsec offload on a network adapter.")]
        public void EnableNetAdapterIPsecOffload(string name)
        {
            RunPowerShell($"Enable-NetAdapterIPsecOffload -Name \"{name}\"");
        }

        [McpServerTool, Description("Enables large send offload (LSO) on a network adapter.")]
        public void EnableNetAdapterLso(string name)
        {
            RunPowerShell($"Enable-NetAdapterLso -Name \"{name}\"");
        }

        [McpServerTool, Description("Enables packet direct on a network adapter.")]
        public void EnableNetAdapterPacketDirect(string name)
        {
            RunPowerShell($"Enable-NetAdapterPacketDirect -Name \"{name}\"");
        }

        [McpServerTool, Description("Enables power management on a network adapter.")]
        public void EnableNetAdapterPowerManagement(string name)
        {
            RunPowerShell($"Enable-NetAdapterPowerManagement -Name \"{name}\"");
        }

        [McpServerTool, Description("Enables QoS (Quality of Service) on a network adapter.")]
        public void EnableNetAdapterQos(string name)
        {
            RunPowerShell($"Enable-NetAdapterQos -Name \"{name}\"");
        }

        [McpServerTool, Description("Enables receive segment coalescing (RSC) on a network adapter.")]
        public void EnableNetAdapterRsc(string name)
        {
            RunPowerShell($"Enable-NetAdapterRsc -Name \"{name}\"");
        }

        [McpServerTool, Description("Enables receive side scaling (RSS) on a network adapter.")]
        public void EnableNetAdapterRss(string name)
        {
            RunPowerShell($"Enable-NetAdapterRss -Name \"{name}\"");
        }

        [McpServerTool, Description("Enables single root I/O virtualization (SR-IOV) on a network adapter.")]
        public void EnableNetAdapterSriov(string name)
        {
            RunPowerShell($"Enable-NetAdapterSriov -Name \"{name}\"");
        }

        [McpServerTool, Description("Enables virtual machine queue (VMQ) on a network adapter.")]
        public void EnableNetAdapterVmq(string name)
        {
            RunPowerShell($"Enable-NetAdapterVmq -Name \"{name}\"");
        }

        [McpServerTool, Description("Gets the basic network adapter properties.")]
        public PSDataCollection<PSObject> GetNetAdapter(string name = null)
        {
            string cmd = string.IsNullOrWhiteSpace(name) ? "Get-NetAdapter" : $"Get-NetAdapter -Name \"{name}\"";
            return RunPowerShell(cmd);
        }

        [McpServerTool, Description("Gets the advanced properties for a network adapter.")]
        public PSDataCollection<PSObject> GetNetAdapterAdvancedProperty(string name)
        {
            return RunPowerShell($"Get-NetAdapterAdvancedProperty -Name \"{name}\"");
        }

        [McpServerTool, Description("Gets a list of bindings for a network adapter.")]
        public PSDataCollection<PSObject> GetNetAdapterBinding(string name)
        {
            return RunPowerShell($"Get-NetAdapterBinding -Name \"{name}\"");
        }

        [McpServerTool, Description("Gets the various checksum offload settings from network adapters that support these checksum offloads.")]
        public PSDataCollection<PSObject> GetNetAdapterChecksumOffload(string name)
        {
            return RunPowerShell($"Get-NetAdapterChecksumOffload -Name \"{name}\"");
        }

        [McpServerTool, Description("Gets the data path configuration for a network adapter.")]
        public PSDataCollection<PSObject> GetNetAdapterDataPathConfiguration(string name)
        {
            return RunPowerShell($"Get-NetAdapterDataPathConfiguration -Name \"{name}\"");
        }

        [McpServerTool, Description("Gets hardware information for a network adapter.")]
        public PSDataCollection<PSObject> GetNetAdapterHardwareInfo(string name)
        {
            return RunPowerShell($"Get-NetAdapterHardwareInfo -Name \"{name}\"");
        }

        [McpServerTool, Description("Gets IPsec offload information for a network adapter.")]
        public PSDataCollection<PSObject> GetNetAdapterIPsecOffload(string name)
        {
            return RunPowerShell($"Get-NetAdapterIPsecOffload -Name \"{name}\"");
        }

        [McpServerTool, Description("Gets the large send offload (LSO) settings for a network adapter.")]
        public PSDataCollection<PSObject> GetNetAdapterLso(string name)
        {
            return RunPowerShell($"Get-NetAdapterLso -Name \"{name}\"");
        }

        [McpServerTool, Description("Gets packet direct information from a network adapter.")]
        public PSDataCollection<PSObject> GetNetAdapterPacketDirect(string name)
        {
            return RunPowerShell($"Get-NetAdapterPacketDirect -Name \"{name}\"");
        }

        [McpServerTool, Description("Gets power management settings for a network adapter.")]
        public PSDataCollection<PSObject> GetNetAdapterPowerManagement(string name)
        {
            return RunPowerShell($"Get-NetAdapterPowerManagement -Name \"{name}\"");
        }

        [McpServerTool, Description("Gets the QoS (Quality of Service) settings for a network adapter.")]
        public PSDataCollection<PSObject> GetNetAdapterQos(string name)
        {
            return RunPowerShell($"Get-NetAdapterQos -Name \"{name}\"");
        }

        [McpServerTool, Description("Gets RDMA (Remote Direct Memory Access) settings for a network adapter.")]
        public PSDataCollection<PSObject> GetNetAdapterRdma(string name)
        {
            return RunPowerShell($"Get-NetAdapterRdma -Name \"{name}\"");
        }

        [McpServerTool, Description("Gets receive segment coalescing (RSC) settings for a network adapter.")]
        public PSDataCollection<PSObject> GetNetAdapterRsc(string name)
        {
            return RunPowerShell($"Get-NetAdapterRsc -Name \"{name}\"");
        }

        [McpServerTool, Description("Gets receive side scaling (RSS) properties of a network adapter.")]
        public PSDataCollection<PSObject> GetNetAdapterRss(string name)
        {
            return RunPowerShell($"Get-NetAdapterRss -Name \"{name}\"");
        }

        [McpServerTool, Description("Gets single root I/O virtualization (SR-IOV) information for a network adapter.")]
        public PSDataCollection<PSObject> GetNetAdapterSriov(string name)
        {
            return RunPowerShell($"Get-NetAdapterSriov -Name \"{name}\"");
        }

        [McpServerTool, Description("Gets single root I/O virtualization (SR-IOV) virtual function information for a network adapter.")]
        public PSDataCollection<PSObject> GetNetAdapterSriovVf(string name)
        {
            return RunPowerShell($"Get-NetAdapterSriovVf -Name \"{name}\"");
        }

        [McpServerTool, Description("Gets network adapter statistics.")]
        public PSDataCollection<PSObject> GetNetAdapterStatistics(string name)
        {
            return RunPowerShell($"Get-NetAdapterStatistics -Name \"{name}\"");
        }

        [McpServerTool, Description("Gets virtual machine queue (VMQ) information for a network adapter.")]
        public PSDataCollection<PSObject> GetNetAdapterVmq(string name)
        {
            return RunPowerShell($"Get-NetAdapterVmq -Name \"{name}\"");
        }

        [McpServerTool, Description("Gets virtual port information from a network adapter.")]
        public PSDataCollection<PSObject> GetNetAdapterVPort(string name)
        {
            return RunPowerShell($"Get-NetAdapterVPort -Name \"{name}\"");
        }

        [McpServerTool, Description("Creates a new network adapter advanced property.")]
        public void NewNetAdapterAdvancedProperty(string name, string displayName, string displayValue)
        {
            RunPowerShell($"New-NetAdapterAdvancedProperty -Name \"{name}\" -DisplayName \"{displayName}\" -DisplayValue \"{displayValue}\"");
        }

        [McpServerTool, Description("Removes a network adapter.")]
        public void RemoveNetAdapter(string name)
        {
            RunPowerShell($"Remove-NetAdapter -Name \"{name}\" -Confirm:$false");
        }

        [McpServerTool, Description("Removes a network adapter advanced property.")]
        public void RemoveNetAdapterAdvancedProperty(string name, string displayName)
        {
            RunPowerShell($"Remove-NetAdapterAdvancedProperty -Name \"{name}\" -DisplayName \"{displayName}\"");
        }

        [McpServerTool, Description("Renames a network adapter.")]
        public void RenameNetAdapter(string name, string newName)
        {
            RunPowerShell($"Rename-NetAdapter -Name \"{name}\" -NewName \"{newName}\"");
        }

        [McpServerTool, Description("Resets a network adapter advanced property to its default value.")]
        public void ResetNetAdapterAdvancedProperty(string name, string displayName)
        {
            RunPowerShell($"Reset-NetAdapterAdvancedProperty -Name \"{name}\" -DisplayName \"{displayName}\"");
        }

        [McpServerTool, Description("Restarts a network adapter.")]
        public void RestartNetAdapter(string name)
        {
            RunPowerShell($"Restart-NetAdapter -Name \"{name}\"");
        }

        [McpServerTool, Description("Sets the basic properties of a network adapter such as VLAN identifier (ID) and MAC address.")]
        public void SetNetAdapter(string name, int? vlanId = null, string macAddress = null)
        {
            string cmd = $"Set-NetAdapter -Name \"{name}\"";
            if (vlanId.HasValue) cmd += $" -VlanID {vlanId.Value}";
            if (!string.IsNullOrWhiteSpace(macAddress)) cmd += $" -MacAddress \"{macAddress}\"";
            RunPowerShell(cmd);
        }

        [McpServerTool, Description("Sets the advanced properties for a network adapter.")]
        public void SetNetAdapterAdvancedProperty(string name, string displayName, string displayValue)
        {
            RunPowerShell($"Set-NetAdapterAdvancedProperty -Name \"{name}\" -DisplayName \"{displayName}\" -DisplayValue \"{displayValue}\"");
        }

        [McpServerTool, Description("Sets the binding state for a network adapter.")]
        public void SetNetAdapterBinding(string name, string componentId, bool enabled)
        {
            string state = enabled ? "Enable-NetAdapterBinding" : "Disable-NetAdapterBinding";
            RunPowerShell($"{state} -Name \"{name}\" -ComponentID \"{componentId}\"");
        }

        [McpServerTool, Description("Sets the various checksum offload settings for a network adapter.")]
        public void SetNetAdapterChecksumOffload(string name, string feature, string value)
        {
            RunPowerShell($"Set-NetAdapterChecksumOffload -Name \"{name}\" -Feature \"{feature}\" -Value \"{value}\"");
        }

        [McpServerTool, Description("Sets the data path configuration for a network adapter.")]
        public void SetNetAdapterDataPathConfiguration(string name, string profile)
        {
            RunPowerShell($"Set-NetAdapterDataPathConfiguration -Name \"{name}\" -Profile \"{profile}\"");
        }

        [McpServerTool, Description("Sets IPsec offload settings for a network adapter.")]
        public void SetNetAdapterIPsecOffload(string name, string value)
        {
            RunPowerShell($"Set-NetAdapterIPsecOffload -Name \"{name}\" -Value \"{value}\"");
        }

        [McpServerTool, Description("Sets large send offload (LSO) for a network adapter.")]
        public void SetNetAdapterLso(string name, string value)
        {
            RunPowerShell($"Set-NetAdapterLso -Name \"{name}\" -Value \"{value}\"");
        }

        [McpServerTool, Description("Sets packet direct on a network adapter.")]
        public void SetNetAdapterPacketDirect(string name, string value)
        {
            RunPowerShell($"Set-NetAdapterPacketDirect -Name \"{name}\" -Value \"{value}\"");
        }

        [McpServerTool, Description("Sets power management for a network adapter.")]
        public void SetNetAdapterPowerManagement(string name, string value)
        {
            RunPowerShell($"Set-NetAdapterPowerManagement -Name \"{name}\" -Value \"{value}\"");
        }

        [McpServerTool, Description("Sets Quality of Service (QoS) for a network adapter.")]
        public void SetNetAdapterQos(string name, string value)
        {
            RunPowerShell($"Set-NetAdapterQos -Name \"{name}\" -Value \"{value}\"");
        }

        [McpServerTool, Description("Sets RDMA (Remote Direct Memory Access) for a network adapter.")]
        public void SetNetAdapterRdma(string name, string value)
        {
            RunPowerShell($"Set-NetAdapterRdma -Name \"{name}\" -Value \"{value}\"");
        }

        [McpServerTool, Description("Sets receive segment coalescing (RSC) for a network adapter.")]
        public void SetNetAdapterRsc(string name, string value)
        {
            RunPowerShell($"Set-NetAdapterRsc -Name \"{name}\" -Value \"{value}\"");
        }

        [McpServerTool, Description("Sets receive side scaling (RSS) for a network adapter.")]
        public void SetNetAdapterRss(string name, string value)
        {
            RunPowerShell($"Set-NetAdapterRss -Name \"{name}\" -Value \"{value}\"");
        }

        [McpServerTool, Description("Sets single root I/O virtualization (SR-IOV) for a network adapter.")]
        public void SetNetAdapterSriov(string name, string value)
        {
            RunPowerShell($"Set-NetAdapterSriov -Name \"{name}\" -Value \"{value}\"");
        }

        [McpServerTool, Description("Sets virtual machine queue (VMQ) for a network adapter.")]
        public void SetNetAdapterVmq(string name, string value)
        {
            RunPowerShell($"Set-NetAdapterVmq -Name \"{name}\" -Value \"{value}\"");
        }
    }
}
