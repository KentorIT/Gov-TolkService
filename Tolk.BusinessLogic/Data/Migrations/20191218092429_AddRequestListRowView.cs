using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddRequestListRowView : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql(@"Create VIEW RequestListRows
As

Select 
	1 'RowType',
	r.RequestId 'EntityId',
	r.ExpiresAt,
	coalesce(l.Name, o.OtherLanguage) 'LanguageName',
	o.LanguageId,
    o.OrderNumber 'EntityNumber',
    og.OrderGroupNumber 'EntityParentNumber',
	re.Name 'RegionName',
	o.RegionId,
	o.StartAt,
	o.EndAt,
	o.Status,
	ra.BrokerId,
	r.CreatedAt,
	c.Name 'CustomerName',
	o.CustomerOrganisationId,
	o.OrderGroupId, 
	o.CustomerReferenceNumber,
	r.AnsweredBy
From Requests r
Join Rankings ra 
On ra.RankingId = r.RankingId
Join Brokers br
On br.BrokerId = ra.BrokerId
Join Orders o 
on o.OrderId = r.OrderId
Join Regions re
On re.RegionId = o.RegionId
Join CustomerOrganisations c
On c.CustomerOrganisationId = o.CustomerOrganisationId
Left Join OrderGroups og
On og.OrderGroupId = o.OrderGroupId
Left Join Languages l
On l.LanguageId = o.LanguageId
Where r.Status Not In(13,17,18)
union
Select 
	2,
	r.RequestGroupId,
	r.ExpiresAt,
	coalesce(l.Name, og.OtherLanguage),
	og.LanguageId,
    og.OrderGroupNumber,
    null,
	re.Name 'RegionName',
	og.RegionId,
	(Select top 1 _o.StartAt From Orders _o Where _o.OrderGroupId = og.OrderGroupId Order By  _o.StartAt ),
	(Select top 1 _o.EndAt From Orders _o Where _o.OrderGroupId = og.OrderGroupId Order By  _o.StartAt ),
	r.Status,
	ra.BrokerId,
	r.CreatedAt,
	c.Name 'CustomerName',
	og.CustomerOrganisationId,
	og.OrderGroupId, 
	(Select top 1 _o.CustomerReferenceNumber From Orders _o Where _o.OrderGroupId = og.OrderGroupId Order By  _o.StartAt ),
	r.AnsweredBy
From RequestGroups r
Join Rankings ra 
On ra.RankingId = r.RankingId
Join Brokers br
On br.BrokerId = ra.BrokerId
Join OrderGroups og 
on og.OrderGroupId = r.OrderGroupId
Join Regions re
On re.RegionId = og.RegionId
Join CustomerOrganisations c
On c.CustomerOrganisationId = og.CustomerOrganisationId
Left Join Languages l
On l.LanguageId = og.LanguageId
Where r.Status Not In(13,17,18)
 ");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql("Drop View RequestListRows");
		}
	}
}
