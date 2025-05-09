CREATE TABLE [dbo].[news] (
    [id]          INT            IDENTITY (1, 1) NOT NULL,
    [newsid]      NVARCHAR (MAX) NULL,
    [category]    NVARCHAR (MAX) NULL,
    [subcategory] NVARCHAR (MAX) NULL,
    [title]       NVARCHAR (MAX) NULL,
    [abstract]    NVARCHAR (MAX) NULL,
    [url]         NVARCHAR (MAX) NULL,
    [etitle]      NVARCHAR (MAX) NULL,
    [eabstract]   NVARCHAR (MAX) NULL,
    PRIMARY KEY CLUSTERED ([id] ASC)
);