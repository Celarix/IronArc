﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronArc.Hardware
{
    public sealed class SystemDevice : HardwareDevice
    {
        public override string DeviceName => "System";
        public override HardwareDeviceStatus Status => HardwareDeviceStatus.Active;
        internal override HardwareDefinitionGenerator.Models.HardwareDevice Definition { get; }

        public SystemDevice(uint deviceId, VirtualMachine vm)
        {
            DeviceId = deviceId;
        }

        public override void HardwareCall(string functionName, VirtualMachine vm)
        {
            // TODO: pass vm as a parameter to the hardware call functions
            string lowerCased = functionName.ToLowerInvariant();

            if (lowerCased == "registerinterrupthandler")
            {
                ulong handlerAddress = vm.Processor.PopExternal(OperandSize.QWord);
                ulong interruptNamePointer = vm.Processor.PopExternal(OperandSize.QWord);
                uint deviceId = (uint)vm.Processor.PopExternal(OperandSize.DWord);

                string interruptName = vm.Processor.ReadStringFromMemory(interruptNamePointer);
                RegisterInterruptHandler(vm, deviceId, interruptName, handlerAddress);
            }
            else if (lowerCased == "unregisterinterrupthandler")
            {
                byte handlerIndex = (byte)vm.Processor.PopExternal(OperandSize.Byte);
                ulong interruptNamePointer = vm.Processor.PopExternal(OperandSize.QWord);
                uint deviceId = (uint)vm.Processor.PopExternal(OperandSize.DWord);

                string interruptName = vm.Processor.ReadStringFromMemory(interruptNamePointer);
                UnregisterInterruptHandler(vm, deviceId, interruptName, handlerIndex);
            }
            else if (lowerCased == "raiseerror")
            {
                uint errorCode = (uint)vm.Processor.PopExternal(OperandSize.DWord);
                RaiseError(vm, errorCode);
            }
            else if (lowerCased == "registererrorhandler")
            {
                ulong handlerAddress = vm.Processor.PopExternal(OperandSize.QWord);
                uint errorCode = (uint)vm.Processor.PopExternal(OperandSize.DWord);
                
                RegisterErrorHandler(vm, errorCode, handlerAddress);
            }
            else if (lowerCased == "unregistererrorhandler")
            {
                uint errorCode = (uint)vm.Processor.PopExternal(OperandSize.DWord);
                
                UnregisterErrorHandler(vm, errorCode);
            }
            else if (lowerCased == "getlasterrordescriptionsize")
            {
                ulong size = GetLastErrorDescriptionSize(vm);
                vm.Processor.PushExternal(size, OperandSize.QWord);
            }
            else if (lowerCased == "getlasterrordescription")
            {
                ulong destination = vm.Processor.PopExternal(OperandSize.QWord);
                GetLastErrorDescription(vm, destination);
            }
        }

        private void RegisterInterruptHandler(VirtualMachine vm, uint deviceId, string interruptName, ulong handlerAddress)
        {
            if (vm.Hardware.All(h => h.DeviceId != deviceId))
            {
                vm.Processor.RaiseError(Error.HardwareError, $"Cannot register interrupt handler for {deviceId} as no device has that ID.");
                return;
            }
            
            vm.Processor.RegisterInterruptHandler(deviceId, interruptName, handlerAddress);
        }

        private void UnregisterInterruptHandler(VirtualMachine vm, uint deviceId, string interruptName, byte handlerIndex)
        {
            if (vm.Hardware.All(h => h.DeviceId != deviceId))
            {
                vm.Processor.RaiseError(Error.HardwareError,
                    $"Cannot unregister interrupt handler for {deviceId} as no device has that ID.");

                return;
            }
            
            vm.Processor.UnregisterInterruptHandler(deviceId, interruptName, handlerIndex);
        }

        private void RaiseError(VirtualMachine vm, uint errorCode)
        {
            vm.Processor.RaiseError(errorCode, null);
        }

        private void RegisterErrorHandler(VirtualMachine vm, uint errorCode, ulong handlerAddress)
        {
            vm.Processor.RegisterErrorHandler(errorCode, handlerAddress);
        }

        private void UnregisterErrorHandler(VirtualMachine vm, uint errorCode)
        {
            vm.Processor.UnregisterErrorHandler(errorCode);
        }

        private ulong GetLastErrorDescriptionSize(VirtualMachine vm) => vm.LastError.GetErrorDescriptionSize();

        private void GetLastErrorDescription(VirtualMachine vm, ulong destination)
        {
            var errorDescription = vm.LastError.GetErrorDescription(destination);
            vm.MemoryManager.Write(errorDescription, destination);
        }
        
        // TODO: methods for hardware description generation
        // and yes, you'll have to generate descriptions twice, once for size and again for the description

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public override void Dispose() { }
    }
}