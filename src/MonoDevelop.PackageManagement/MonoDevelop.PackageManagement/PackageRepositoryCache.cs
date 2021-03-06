﻿// 
// PackageRepositoryCache.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2012-2013 Matthew Ward
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class PackageRepositoryCache : IPackageRepositoryCache
	{
		ISharpDevelopPackageRepositoryFactory factory;
		RegisteredPackageSources registeredPackageSources;
		IList<RecentPackageInfo> recentPackages;
		IRecentPackageRepository recentPackageRepository;
		ConcurrentDictionary<string, IPackageRepository> repositories =
			new ConcurrentDictionary<string, IPackageRepository>();
		
		public PackageRepositoryCache(
			ISharpDevelopPackageRepositoryFactory factory,
			RegisteredPackageSources registeredPackageSources,
			IList<RecentPackageInfo> recentPackages)
		{
			this.factory = factory;
			this.registeredPackageSources = registeredPackageSources;
			this.recentPackages = recentPackages;
		}
		
		public PackageRepositoryCache(
			RegisteredPackageSources registeredPackageSources,
			IList<RecentPackageInfo> recentPackages)
			: this(
				new SharpDevelopPackageRepositoryFactory(),
				registeredPackageSources,
				recentPackages)
		{
		}
		
		public IPackageRepository CreateRepository(string packageSource)
		{
			IPackageRepository repository = GetExistingRepository(packageSource);
			if (repository != null) {
				return repository;
			}
			return CreateNewCachedRepository(packageSource);
		}
		
		IPackageRepository GetExistingRepository(string packageSource)
		{
			IPackageRepository repository = null;
			if (repositories.TryGetValue(packageSource, out repository)) {
				return repository;
			}
			return null;
		}
		
		IPackageRepository CreateNewCachedRepository(string packageSource)
		{
			IPackageRepository repository = factory.CreateRepository(packageSource);
			repositories.TryAdd(packageSource, repository);
			return repository;
		}
		
		public ISharedPackageRepository CreateSharedRepository(IPackagePathResolver pathResolver, IFileSystem fileSystem, IFileSystem configSettingsFileSystem)
		{
			return factory.CreateSharedRepository(pathResolver, fileSystem, configSettingsFileSystem);
		}
		
		public IPackageRepository CreateAggregateRepository()
		{
			IEnumerable<IPackageRepository> allRepositories = CreateAllEnabledRepositories();
			return CreateAggregateRepository(allRepositories);
		}
		
		IEnumerable<IPackageRepository> CreateAllEnabledRepositories()
		{
			foreach (PackageSource source in registeredPackageSources.GetEnabledPackageSources()) {
				yield return CreateRepository(source.Source);
			}
		}
		
		public IPackageRepository CreateAggregateRepository(IEnumerable<IPackageRepository> repositories)
		{
			return factory.CreateAggregateRepository(repositories);
		}
		
		public IRecentPackageRepository RecentPackageRepository {
			get {
				CreateRecentPackageRepository();
				return recentPackageRepository;
			}
		}
		
		void CreateRecentPackageRepository()
		{
			if (recentPackageRepository == null) {
				IPackageRepository aggregateRepository = CreateAggregateRepository();
				CreateRecentPackageRepository(recentPackages, aggregateRepository);
			}
		}
		
		public IRecentPackageRepository CreateRecentPackageRepository(
			IList<RecentPackageInfo> recentPackages,
			IPackageRepository aggregateRepository)
		{
			if (recentPackageRepository == null) {
				recentPackageRepository = factory.CreateRecentPackageRepository(recentPackages, aggregateRepository);
			}
			return recentPackageRepository;
		}
	}
}
