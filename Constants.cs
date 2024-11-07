using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbakConfigurator.IEC
{
    public static class Constants
    {
        private static Dictionary<DataType, int> IECDataTypeSizes { get; } = new Dictionary<DataType, int>
        {
            { DataType.M_SP_NA_1, 2 },
            { DataType.M_SP_TA_1, 2 },
            { DataType.M_DP_NA_1, 2 },
            { DataType.M_DP_TA_1, 2 },
            { DataType.M_ST_NA_1, 2 },
            { DataType.M_ST_TA_1, 2 },
            { DataType.M_BO_NA_1, 4 },
            { DataType.M_BO_TA_1, 4 },
            { DataType.M_ME_NA_1, 2 },
            { DataType.M_ME_TA_1, 2 },
            { DataType.M_ME_NB_1, 2 },
            { DataType.M_ME_TB_1, 2 },
            { DataType.M_ME_NC_1, 4 },
            { DataType.M_ME_TC_1, 4 },
            { DataType.M_IT_NA_1, 4 },
            { DataType.M_IT_TA_1, 4 },
            // В будущем если понадобятся
            //{ DataType.M_EP_TA_1, 2 },
            //{ DataType.M_EP_TB_1, 2 },
            //{ DataType.M_EP_TC_1, 2 },
            //{ DataType.M_PS_NA_1, 2 },
            { DataType.M_ME_ND_1, 2 },

            { DataType.M_SP_TB_1, 2 },
            { DataType.M_DP_TB_1, 2 },
            { DataType.M_ST_TB_1, 2 },
            { DataType.M_BO_TB_1, 4 },
            { DataType.M_ME_TD_1, 2 },
            { DataType.M_ME_TE_1, 2 },
            { DataType.M_ME_TF_1, 4 },
            { DataType.M_IT_TB_1, 4 },
            // В будущем если понадобятся
            //{ DataType.M_EP_TD_1, 2 },
            //{ DataType.M_EP_TE_1, 2 },
            //{ DataType.M_EP_TF_1, 2 },

            // commands
            { DataType.C_SC_NA_1, 2 },
            { DataType.C_DC_NA_1, 2 },
            { DataType.C_RC_NA_1, 2 },
            { DataType.C_SE_NA_1, 2 },
            { DataType.C_SE_NB_1, 2 },
            { DataType.C_SE_NC_1, 4 },
            { DataType.C_BO_NA_1, 4 },
            { DataType.C_SC_TA_1, 2 },
            { DataType.C_DC_TA_1, 2 },
            { DataType.C_RC_TA_1, 2 },
            { DataType.C_SE_TA_1, 2 },
            { DataType.C_SE_TB_1, 2 },
            { DataType.C_SE_TC_1, 4 },
            { DataType.C_BO_TA_1, 4 },

            { DataType.M_IT_ND_1, 8 },
            { DataType.M_IT_TD_1, 8 },
            { DataType.M_ME_NO_1, 8 },
            { DataType.M_ME_TO_1, 8 },
            { DataType.M_ME_NX_1, 8 },
            { DataType.M_ME_TX_1, 8 },
        };

        public static int ModbusRegisterSizeInBytes = 2;

        public static int IECDataTypeSizeInBytes(DataType dataType)
        {
            return IECDataTypeSizes[dataType];
        }

        public static int DataTypeSizeInModbusRegisters(DataType dataType)
        {
            int dataTypeSize = IECDataTypeSizeInBytes(dataType);
            int numRegisters = dataTypeSize / ModbusRegisterSizeInBytes;
            return numRegisters;
        }

        private static Dictionary<DataType, string> IECDataTypeTooltips { get; } = new Dictionary<DataType, string>
        {
            { DataType.M_SP_NA_1, "Single point information" },
            { DataType.M_SP_TA_1, "Single point information with time tag" },
            { DataType.M_DP_NA_1, "Double point information" },
            { DataType.M_DP_TA_1, "Double point information with time tag" },
            { DataType.M_ST_NA_1, "Step position information" },
            { DataType.M_ST_TA_1, "Step position information with time tag" },
            { DataType.M_BO_NA_1, "Bit string of 32 bit" },
            { DataType.M_BO_TA_1, "Bit string of 32 bit with time tag" },
            { DataType.M_ME_NA_1, "Measured value, normalized value " },
            { DataType.M_ME_TA_1, "Time tag" },
            { DataType.M_ME_NB_1, "Measured value, scaled value" },
            { DataType.M_ME_TB_1, "Measured value, scaled value with time tag" },
            { DataType.M_ME_NC_1, "Measured value, short floating point value " },
            { DataType.M_ME_TC_1, "Measured value, short floating point value with time tag" },
            { DataType.M_IT_NA_1, "Integrated totals" },
            { DataType.M_IT_TA_1, "Integrated totals with time tag" },
            //{ DataType.M_EP_TA_1, "Event of protection equipment with time tag" },
            // В будущем если понадобятся
            //{ DataType.M_EP_TB_1, "Packed start events of protection equipment with time tag" },
            //{ DataType.M_EP_TC_1, "Packed output circuit information of protection equipment with time tag" },
            //{ DataType.M_PS_NA_1, "Packed single-point information with status change detection" },
            { DataType.M_ME_ND_1, "Measured value, normalized value without quality descriptor" },
            { DataType.M_SP_TB_1, "Single point information with time tag CP56Time2a" },
            { DataType.M_DP_TB_1, "Double point information with time tag CP56Time2a" },
            { DataType.M_ST_TB_1, "Step position information with time tag CP56Time2a" },
            { DataType.M_BO_TB_1, "Bit string of 32 bit with time tag CP56Time2a" },
            { DataType.M_ME_TD_1, "Measured value, normalized value with time tag CP56Time2a" },
            { DataType.M_ME_TE_1, "Measured value, scaled value with time tag CP56Time2a" },
            { DataType.M_ME_TF_1, "Measured value, short floating point value with time tag CP56Time2a" },
            { DataType.M_IT_TB_1, "Integrated totals with time tag CP56Time2a" },
            // В будущем если понадобятся
            //{ DataType.M_EP_TD_1, "Event of protection equipment with time tag CP56Time2a" },
            //{ DataType.M_EP_TE_1, "Packed start events of protection equipment with time tag CP56time2a" },
            //{ DataType.M_EP_TF_1, "Packed output circuit information of protection equipment with time tag CP56Time2a" },
            
            // commands
            { DataType.C_SC_NA_1, "Single command" },
            { DataType.C_DC_NA_1, "Double command" },
            { DataType.C_RC_NA_1, "Regulating step command" },
            { DataType.C_SE_NA_1, "Setpoint command, normalized value" },
            { DataType.C_SE_NB_1, "Setpoint command, scaled value" },
            { DataType.C_SE_NC_1, "Setpoint command, short floating point value" },
            { DataType.C_BO_NA_1, "Bit string 32 bit" },
            { DataType.C_SC_TA_1, "Single command with time tag CP56Time2a" },
            { DataType.C_DC_TA_1, "Double command with time tag CP56Time2a" },
            { DataType.C_RC_TA_1, "Regulating step command with time tag CP56Time2a" },
            { DataType.C_SE_TA_1, "Setpoint command, normalized value with time tag CP56Time2a" },
            { DataType.C_SE_TB_1, "Setpoint command, scaled value with time tag CP56Time2a" },
            { DataType.C_SE_TC_1, "Setpoint command, short floating point value with time tag CP56Time2a" },
            { DataType.C_BO_TA_1, "Bit string 32 bit with time tag CP56Time2a" },

            { DataType.M_IT_ND_1, "Double 64 bit" },
            { DataType.M_IT_TD_1, "Double 64 bit" },
            { DataType.M_ME_NO_1, "Int 64 bit" },
            { DataType.M_ME_TO_1, "Int 64 bit" },
            { DataType.M_ME_NX_1, "UInt 64 bit" },
            { DataType.M_ME_TX_1, "UInt 64 bit" },
        };

        public static string IECDataTypeTooltip(DataType dataType)
        {
            return IECDataTypeTooltips[dataType];
        }

        private static List<DataType> _normalizeTypes = new List<DataType>()
        {
            DataType.M_ME_NA_1, DataType.M_ME_TA_1, DataType.M_ME_TD_1, DataType.M_ME_ND_1
        };

        public static float ToNormalizeValueDeadband(DataType dataType, float deadband)
        {
            if (_normalizeTypes.Contains(dataType))
            {
                // 65535/2*x
                return 65535 / 2.0f * deadband;
            }
            return deadband;
        }

        public static float FromNormalizeValueDeadband(DataType dataType, float deadband)
        {
            if (_normalizeTypes.Contains(dataType))
            {
                return deadband / (65535 / 2.0f);
            }
            return deadband;
        }
    }
}
