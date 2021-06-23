# code-sample

I added some files from one of the most recents projects I worked on. There are a samples of:
- controller: the design decision made was to flatten the architecture, and only using Services when it made sense to extract that logic it's own component. For example, we needed to use the same method in other place.
- service: dependency injection was used so I provied it's interface too. This is an example of a piece of code we used in several parts of the solution. Sending push notifications to the different apps. We had two kind of notifications mass and personal notification. The first was sent to the applications that were subscribed to the specific topic (Audience or Musician). The second was sent to all the devices the user had registered, so we had to keep also the relationship between user and device id stored in the DB.
- repository: an example of how we accessed the data stored in SQL Server using Linq and Entity Framework as ORM.
