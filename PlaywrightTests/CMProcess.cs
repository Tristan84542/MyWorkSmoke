namespace PlaywrightTests;

public class CMProcess
{
    public string Pid { get; set; }
    public string PName { get; set; }
    public string STime { get; set; }
    public string Sup { get; set; }
    public string Cust { get; set; }
    public string State { get; set; } 

    public CMProcess (string id,  string name, string time, string sup, string cust, string state)
    {
        Pid = id;
        PName = name;
        STime = time;
        Sup = sup; 
        Cust = cust;
        State = state;
    }
}