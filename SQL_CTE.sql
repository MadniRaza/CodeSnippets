	WITH MtlMakePartsTree (
		Recnum
		,ID
		,Company
		,Plant
		,ProdCode
		,PartNum
		,MtlPartNum
		,PartDesc
		,TypeCode
		,ForeQty
		,OurLT
		,StartDate
		,ForeDate
		,CreatedDate
		,CreatedBy
		,Lvl
		,Heirarchy
		)
	AS (
		SELECT M.RecNum
			,CAST(M.Recnum AS VARCHAR(max)) + '.' + CAST(ROW_NUMBER() OVER (
					PARTITION BY M.Recnum ORDER BY M.Recnum
					) AS VARCHAR(MAX))
			,M.Company
			,M.Plant
			,P.ProdCode
			,M.PartNum
			,PM.MtlPartNum
			,P.PartDescription
			,CAST(P.TypeCode AS CHAR(1))
			,CAST(CEILING(M.ForeQty * ISNULL(PA.UQP, 0)) AS INT)
			,CASE 
				WHEN ISNULL(PA.LeadTime, 0) = 0
					THEN 99
				ELSE PA.LeadTime
				END
			,DATEADD(d, - CASE 
					WHEN ISNULL(PA.LeadTime, 0) = 0
						THEN 99
					ELSE PA.LeadTime
					END, M.StartDate)
			,M.StartDate AS ForeDate
			,M.CreatedDate
			,M.CreatedBy
			,2 AS Lvl
			,CAST(M.PartNum + ' < ' + P.PartNum AS NVARCHAR(MAX))
		FROM zMP_DmndMst M
		INNER JOIN Erp.PartMtl PM ON PM.Company = M.Company
			AND PM.PartNum = M.PartNum
			AND PM.QtyPer > 0
		INNER JOIN Erp.Part P ON P.Company = M.Company
			AND PM.MtlPartnum = P.PartNum
			AND P.TypeCode = 'M'
		LEFT OUTER JOIN zMP_Part_Attributes PA ON PA.Company = P.Company
			AND PA.PartNum = P.PartNum
		
		UNION ALL
		
		SELECT ParentNode.Recnum
			,ParentNOde.ID + '.' + CAST(ROW_NUMBER() OVER (
					ORDER BY ParentNode.RecNum
					) AS VARCHAR(MAX))
			,ParentNode.Company
			,ParentNode.Plant
			,P.ProdCode
			,CAST(PM.PartNum AS NVARCHAR(50)) AS PartNum
			,PM.MtlPartNum
			,P.PartDescription
			,CAST(P.TypeCode AS CHAR(1))
			,CAST(CEILING(ParentNode.ForeQty * ISNULL(PA.UQP, 0)) AS INT) AS ForeQty
			,CASE 
				WHEN ISNULL(PA.LeadTime, 0) = 0
					THEN 99
				ELSE PA.LeadTime
				END
			,DATEADD(d, - CASE 
					WHEN ISNULL(PA.LeadTime, 0) = 0
						THEN 99
					ELSE PA.LeadTime
					END, ParentNode.StartDate)
			,ParentNode.StartDate AS ForeDate
			,ParentNode.CreatedDate
			,ParentNode.CreatedBy
			,Lvl + 1
			,Heirarchy + ' < ' + PM.MtlPartNum
		FROM Erp.PartMtl PM
		INNER JOIN Erp.Part P ON P.Company = PM.Company
			AND P.PartNum = PM.MtlPartNum
			AND P.TypeCode = 'M'
		INNER JOIN MtlMakePartsTree ParentNode ON PM.PartNum = ParentNode.MtlPartNum
			AND PM.QtyPer > 0
		INNER JOIN zMP_Part_Attributes PA ON PA.Company = P.Company
			AND PA.PartNum = P.PartNum
		)
		SELECT * FROM MtlMakePartsTree
	ORDER BY DBO.SortNumber(ID)


create function [dbo].[SortNumber](@indexnum varchar(50))
returns varchar(50)
as
begin
declare @curlength int, @length int
declare @rtnstring varchar(50)
select @rtnstring = '', @curlength = 0
while (@curlength < len(@indexnum)) -- loop until reach the last "." mark
begin
set @length = charindex('.', @indexnum, @curlength + 1)
if @length = 0
set @length = len(@indexnum) + 1
-- pading with leading zero
set @rtnstring = @rtnstring + right('0000' + substring(@indexnum, @curlength + 1, @length - @curlength - 1), 4) + '.'
set @curlength = @length
end
return @rtnstring
end




