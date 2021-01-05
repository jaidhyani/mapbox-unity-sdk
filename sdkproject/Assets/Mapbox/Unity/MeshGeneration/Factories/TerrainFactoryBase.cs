﻿using Mapbox.Unity.MeshGeneration.Factories;
using System.Collections;
using UnityEngine;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.Map;
using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Enums;
using Mapbox.Unity.MeshGeneration.Factories.TerrainStrategies;
using System;
using System.Collections.Generic;

namespace Mapbox.Unity.MeshGeneration.Factories
{
	public class TerrainFactoryBase : AbstractTileFactory
	{
		public TerrainStrategy Strategy;
		[SerializeField]
		protected ElevationLayerProperties _elevationOptions = new ElevationLayerProperties();
		protected TerrainDataFetcher DataFetcher;

		public TerrainDataFetcher GetFetcher()
		{
			return DataFetcher;
		}

		public string TilesetId
		{
			get
			{
				return _elevationOptions.sourceOptions.Id;
			}

			set
			{
				_elevationOptions.sourceOptions.Id = value;
			}
		}

		public ElevationLayerProperties Properties
		{
			get
			{
				return _elevationOptions;
			}
		}

		#region UnityMethods
		private void OnDestroy()
		{
			if (DataFetcher != null)
			{
				DataFetcher.TextureReceived -= OnTerrainRecieved;
				DataFetcher.FetchingError -= OnDataError;
			}
		}
		#endregion

		#region AbstractFactoryOverrides
		protected override void OnInitialized()
		{
			Strategy.Initialize(_elevationOptions);
			DataFetcher = new TerrainDataFetcher();
			DataFetcher.TextureReceived += OnTerrainRecieved;
			DataFetcher.FetchingError += OnDataError;
		}

		public override void SetOptions(LayerProperties options)
		{
			_elevationOptions = (ElevationLayerProperties)options;
			Strategy.Initialize(_elevationOptions);
		}

		protected override void OnRegistered(UnityTile tile)
		{
			if (Properties.sourceType == ElevationSourceType.None)
			{
				tile.SetHeightData(TilesetId,null);
				tile.MeshFilter.sharedMesh.Clear();
				tile.ElevationType = TileTerrainType.None;
				tile.HeightDataState = TilePropertyState.None;
				return;
			}

			if (Strategy is IElevationBasedTerrainStrategy)
			{
				tile.HeightDataState = TilePropertyState.Loading;
				TerrainDataFetcherParameters parameters = new TerrainDataFetcherParameters()
				{
					canonicalTileId = tile.CanonicalTileId,
					tilesetId = _elevationOptions.sourceOptions.Id,
					tile = tile
				};
				DataFetcher.FetchData(parameters);
			}
			else
			{
				//reseting height data
				tile.SetHeightData(TilesetId, null);
				Strategy.RegisterTile(tile, false);
				tile.HeightDataState = TilePropertyState.Loaded;
			}
		}

		protected override void OnUnregistered(UnityTile tile)
		{
			DataFetcher.CancelFetching(tile.UnwrappedTileId, TilesetId);
			if (_tilesWaitingResponse != null && _tilesWaitingResponse.Contains(tile))
			{
				_tilesWaitingResponse.Remove(tile);
			}
			Strategy.UnregisterTile(tile);
		}

		public override void Clear()
		{
			//DestroyImmediate(DataFetcher);
		}

		protected override void OnPostProcess(UnityTile tile)
		{
			Strategy.PostProcessTile(tile);
		}

		public override void UnbindEvents()
		{
			base.UnbindEvents();
		}

		protected override void OnUnbindEvents()
		{
		}
		#endregion

		#region DataFetcherEvents

		private void OnTerrainRecieved(UnityTile tile, Texture2D texture)
		{
			if (tile != null)
			{
				_tilesWaitingResponse.Remove(tile);

				if (tile.HeightDataState != TilePropertyState.Unregistered)
				{
					if (texture != null)
					{
						//if collider is disabled, we switch to a shader based solution
						//no elevated mesh is generated
						if (!_elevationOptions.colliderOptions.addCollider)
						{
							tile.SetHeightTextureForShader(TilesetId, texture, _elevationOptions.requiredOptions.exaggerationFactor, _elevationOptions.modificationOptions.useRelativeHeight, _elevationOptions.colliderOptions.addCollider);
							Strategy.RegisterTile(tile, false);
						}
						else
						{
							tile.SetHeightTexture(TilesetId, texture, _elevationOptions.requiredOptions.exaggerationFactor, _elevationOptions.modificationOptions.useRelativeHeight, _elevationOptions.colliderOptions.addCollider);
							Strategy.RegisterTile(tile, true);
						}
					}
				}
			}
		}

		private void OnTerrainRecieved(UnityTile tile, RasterTile pngRasterTile)
		{
			if (tile != null)
			{
				_tilesWaitingResponse.Remove(tile);

				if (tile.HeightDataState != TilePropertyState.Unregistered)
				{
					if (pngRasterTile.Texture2D != null)
					{
						//if collider is disabled, we switch to a shader based solution
						//no elevated mesh is generated
						if (!_elevationOptions.colliderOptions.addCollider)
						{
							tile.SetHeightTextureForShader(TilesetId, pngRasterTile.Texture2D, _elevationOptions.requiredOptions.exaggerationFactor, _elevationOptions.modificationOptions.useRelativeHeight, _elevationOptions.colliderOptions.addCollider);
							Strategy.RegisterTile(tile, false);
						}
						else
						{
							tile.SetHeightTexture(TilesetId, pngRasterTile.Texture2D, _elevationOptions.requiredOptions.exaggerationFactor, _elevationOptions.modificationOptions.useRelativeHeight, _elevationOptions.colliderOptions.addCollider);
							Strategy.RegisterTile(tile, true);
						}
					}
					else
					{
						//tile.SetHeightData(pngRasterTile.Data, _elevationOptions.requiredOptions.exaggerationFactor, _elevationOptions.modificationOptions.useRelativeHeight, _elevationOptions.colliderOptions.addCollider);
						//tile.SetElevationData(pngRasterTile.Elevation, _elevationOptions.requiredOptions.exaggerationFactor, _elevationOptions.modificationOptions.useRelativeHeight, _elevationOptions.colliderOptions.addCollider);
						//Strategy.RegisterTile(tile);
					}
				}
			}
		}

		private void OnDataError(UnityTile tile, RasterTile rawTile, TileErrorEventArgs e)
		{
			base.OnErrorOccurred(tile, e);
			if (tile != null)
			{
				_tilesWaitingResponse.Remove(tile);
				if (tile.HeightDataState != TilePropertyState.Unregistered)
				{
					Strategy.DataErrorOccurred(tile, e);
					tile.HeightDataState = TilePropertyState.Error;
				}
			}
		}
		#endregion

	}
}
