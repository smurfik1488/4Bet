namespace _4Bet.Application.DTOs;

public class TeamImportResultDto
{
    public int TotalRows { get; set; }
    public int UniqueRows { get; set; }
    public int InsertedRows { get; set; }
    public int ExistingRows { get; set; }
    public int InvalidRows { get; set; }
}
