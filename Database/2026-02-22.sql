-- Add new columns to Accounts table
ALTER TABLE [dbo].[Accounts] ADD [AccountGroup] nvarchar(100) NULL;
ALTER TABLE [dbo].[Accounts] ADD [AccountType] nvarchar(100) NULL;
ALTER TABLE [dbo].[Accounts] ADD [ParentAccountId] int NULL;

-- Add self-referencing foreign key for hierarchical structure
ALTER TABLE [dbo].[Accounts]  WITH CHECK ADD  CONSTRAINT [FK_Accounts_Accounts] FOREIGN KEY([ParentAccountId])
REFERENCES [dbo].[Accounts] ([Id])
GO

ALTER TABLE [dbo].[Accounts] CHECK CONSTRAINT [FK_Accounts_Accounts]
GO
