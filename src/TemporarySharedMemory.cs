using Atomus.Diagnostics;
using System;
using System.Data;

namespace Atomus.Service
{
    public class TemporarySharedMemory : IService
    {
        private static System.Collections.Hashtable hashtable;
        private static System.Timers.Timer timer;

        static TemporarySharedMemory()
        {
            hashtable = new System.Collections.Hashtable();

            timer = new System.Timers.Timer();
            timer.Elapsed += Timer_Tick;
        }

        public TemporarySharedMemory()
        {
            try
            {
                timer.Interval = this.GetAttribute("ExpiryInterval").ToInt();
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(exception);
                timer.Interval = 60000;
            }

            timer.Start();
        }

        Response IService.Request(ServiceDataSet serviceDataSet)
        {
            IResponse response;
            DataTable dataTable;
            DataRow dataRow;
            int tableCount;
            SharedObject sharedObject;

            try
            {
                if (!serviceDataSet.ServiceName.Equals("Atomus.Service.TemporarySharedMemory"))
                    throw new Exception("Not Atomus.Service.TemporarySharedMemory");

                response = (IResponse)Factory.CreateInstance("Atomus.Service.Response", false, true);


                tableCount = 0;
                foreach (DataTable table in ((IServiceDataSet)serviceDataSet).DataTables)
                {
                    switch (table.ExtendedProperties["Action"].ToString())
                    {
                        case "Get":
                            dataTable = response.DataSet.Tables.Add(tableCount.ToString());

                            dataTable.Columns.Add("Key", Type.GetType("System.String"));
                            dataTable.Columns.Add("Data", Type.GetType("System.Byte[]"));
                            dataTable.Columns.Add("ExpiryDateTime", Type.GetType("System.DateTime"));
                            dataTable.Columns.Add("MaxReadCount", Type.GetType("System.Int32"));

                            foreach (DataRow dataRow1 in table.Rows)
                            {
                                sharedObject = (SharedObject)hashtable[(string)dataRow1["Key"]];

                                dataRow = dataTable.NewRow();

                                if (sharedObject != null)
                                {
                                    dataRow["Key"] = sharedObject.Key;
                                    dataRow["Data"] = (byte[])sharedObject.Object;
                                    dataRow["ExpiryDateTime"] = sharedObject.ExpiryDateTime;
                                    dataRow["MaxReadCount"] = sharedObject.MaxReadCount;

                                    sharedObject.ReadCount += 1;

                                    if (sharedObject.ReadCount >= sharedObject.MaxReadCount)
                                        hashtable.Remove((string)dataRow1["Key"]);
                                }

                                dataTable.Rows.Add(dataRow);
                            }

                            tableCount += 1;

                            break;

                        case "Set":
                            foreach (DataRow dataRow1 in table.Rows)
                            {
                                sharedObject = (SharedObject)hashtable[(string)dataRow1["Key"]];

                                if (sharedObject == null)
                                {
                                    sharedObject = new SharedObject((string)dataRow1["Key"])
                                    {
                                        ExpiryDateTime = DateTime.Now
                                    };

                                    hashtable.Add(dataRow1["Key"], sharedObject);
                                }

                                sharedObject.Object = dataRow1["Data"];

                                if (dataRow1["ExpiryDateTimeSpan"] != null)
                                    sharedObject.ExpiryDateTime = sharedObject.ExpiryDateTime.AddMilliseconds((int)dataRow1["ExpiryDateTimeSpan"]);

                                if (dataRow1["MaxReadCount"] != null)
                                    sharedObject.MaxReadCount = (int)dataRow1["MaxReadCount"];
                            }

                            break;

                        default:
                            response.Status = Status.Failed;
                            response.Message = "Not Suport Action Name";
                            return (Response)response;
                    }
                }


                response.Status = Status.OK;
                return (Response)response;
            }
            finally
            {
            }
        }

        private static void Timer_Tick(object sender, System.Timers.ElapsedEventArgs e)
        {
            DateTime dateTime;
            System.Collections.ArrayList array;

            try
            {
                dateTime = DateTime.Now;
                array = new System.Collections.ArrayList();

                foreach (SharedObject _SharedObject in hashtable.Values)
                {
                    if (_SharedObject.ExpiryDateTime < dateTime)
                        array.Add(_SharedObject.Key);
                }

                foreach (SharedObject _SharedObject in hashtable.Values)
                {
                    if (_SharedObject.ReadCount >= _SharedObject.MaxReadCount && !array.Contains(_SharedObject))
                        array.Add(_SharedObject.Key);
                }

                foreach (string key in array.ToArray())
                {
                    hashtable.Remove(key);
                }
            }
            catch (Exception exception)
            {
                DiagnosticsTool.MyTrace(exception);
            }
        }
    }
}
