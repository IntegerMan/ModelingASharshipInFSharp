﻿namespace MattEland.FSharpStarship.Logic

open Positions
open World
open Tiles
open GameObjects
open TiledSharp

module TiledInterop =

  type TiledInfo = 
    {
      Floor: List<TmxLayerTile>
      Walls: List<TmxLayerTile>
      WaterPipes: List<TmxLayerTile>
      AirPipes: List<TmxLayerTile>
      Decorations: List<TmxLayerTile>
      Grating: List<TmxLayerTile>
      Overlays: List<TmxLayerTile>
      Doors: List<TmxLayerTile>
      Objects: List<TmxObject>
    }

  let getTilePos (t: TmxLayerTile) = pos t.X t.Y

  let pixelToTile (v: float): int = (int v) / 32

  let getObjectPos (o: TmxObject) = pos (pixelToTile o.X) (pixelToTile o.Y)

  let allTiledObjects (tilemap: TmxMap) = tilemap.ObjectGroups |> Seq.collect(fun g -> g.Objects) |> Seq.toList

  let objectsOfType typeName tilemap = 
    tilemap
    |> allTiledObjects
    |> List.where(fun o -> o.Type = typeName)

  let mapObjectTypeByFunc typeName createFunc (tilemap: TmxMap) =
    tilemap
    |> objectsOfType typeName
    |> List.map(createFunc)

  let mapObjectType typeName objectType (tilemap: TmxMap) =
    tilemap
    |> mapObjectTypeByFunc typeName (fun obj -> {ObjectType=objectType; Pos=(getObjectPos obj)})

  let getTilesFromLayer (layerName: string) (tilemap: TmxMap) = 
    let layer = tilemap.Layers.[layerName]
    layer.Tiles |> Seq.filter(fun t -> t.Gid > 0) |> Seq.toList
    
  let getObjects (tilemap: TiledSharp.TmxMap): List<GameObject> =
    let astronauts = tilemap |> mapObjectType "Astronaut" Astronaut
    let doors = 
      tilemap 
      |> getTilesFromLayer "Doors" 
      |> List.map(fun d -> {ObjectType=Door(IsOpen=false, IsHorizontal=false); Pos=getTilePos d})

    List.append astronauts doors

  let buildArt gid zindex (tilemap: TmxMap): TileArt =
    let tileset =
      tilemap.Tilesets
      |> Seq.find(fun t -> gid >= t.FirstGid && gid <= (t.FirstGid + (t.TileCount.Value - 1)))

    let index = gid - tileset.FirstGid
    let numColumns = tileset.Columns.Value
    let row = index % numColumns
    let column = index / numColumns

    {
      TileFile = tileset.Image.Source
      X = row * tileset.TileWidth
      Y = column * tileset.TileHeight
      Width = tileset.TileWidth
      Height = tileset.TileHeight
      ZIndex = zindex
    }

  let buildTile (tilemap: TmxMap) tileType (tile: TmxLayerTile) =
    let pos = tile |> getTilePos

    let art = tilemap |> buildArt tile.Gid 0
    makeTile tileType [] [art] pos

  let getTiles (tilemap: TiledSharp.TmxMap): List<Tile> =
    let floorTiles = 
      tilemap
      |> getTilesFromLayer "Floor"
      |> List.map(fun t -> buildTile tilemap Floor t)

    let wallTiles = 
      tilemap
      |> getTilesFromLayer "Walls"
      |> List.map(fun t -> buildTile tilemap Wall t)
    
    List.append floorTiles wallTiles

  let interpretWorld tilemap =
    {
      Floor = tilemap |> getTilesFromLayer "Floor"
      Walls = tilemap |> getTilesFromLayer "Walls"
      Doors = tilemap |> getTilesFromLayer "Doors"
      WaterPipes = tilemap |> getTilesFromLayer "Water"
      AirPipes = tilemap |> getTilesFromLayer "Air"
      Decorations = tilemap |> getTilesFromLayer "Deco"
      Overlays = tilemap |> getTilesFromLayer "Overlays"
      Grating = tilemap |> getTilesFromLayer "Grating"
      Objects = tilemap |> allTiledObjects
    }

  let translateToTile (tilemap: TmxMap) tileType (tmxTile: TmxLayerTile) =

    let art = buildArt tmxTile.Gid 0 tilemap
    tmxTile
    |> getTilePos
    |> makeTile tileType [] [art]

  let translateToObject (tmxObject: TmxObject) = 
    let objectType =
      match tmxObject.Type with
      | "Astronaut" -> Astronaut
    {ObjectType=objectType; Pos=getObjectPos tmxObject}

  let mergeLayerWithTile (nextLayer: seq<Tile>) (tile: Tile) =
    let otherLayerTile = nextLayer |> Seq.tryFind(fun t -> t.Pos = tile.Pos)

    match otherLayerTile with
    | None -> tile
    | Some otherTile -> {tile with Art=tile.Art @ otherTile.Art}

  let getUniqueTiles (sourceLayer: List<Tile>) (otherLayer: List<Tile>) =
    sourceLayer
    |> List.filter(fun t -> 
      otherLayer 
      |> List.tryFind(fun ot -> ot.Pos = t.Pos) 
      |> Option.isNone)

  let mergeWith (nextLayer: List<Tile>) (baseLayer: List<Tile>) = 

    let newTiles = getUniqueTiles nextLayer baseLayer

    baseLayer
    |> List.map (fun t -> mergeLayerWithTile nextLayer t)
    |> List.append newTiles

  let createDoor (walls: seq<Tile>) doorTile =
    let pos = doorTile |> getTilePos

    let isHorizontal = 
      walls
      |> Seq.tryFind(fun w -> w.Pos = (pos |> offset 0 -1))
      |> Option.isSome

    {ObjectType=Door(IsOpen=false, IsHorizontal=isHorizontal); Pos=pos}

  let getObjectsAtPos pos objects = objects |> Seq.filter(fun o -> o.Pos = pos)

  let generateWorld (tilemap: TmxMap) data  =
    let floors = data.Floor |> List.map(fun t -> t |> translateToTile tilemap Floor)
    let walls = data.Walls |> List.map(fun t -> t |> translateToTile tilemap Wall)
    let airPipes = data.AirPipes |> List.map(fun t -> t |> translateToTile tilemap AirPipe)
    let waterPipes = data.WaterPipes |> List.map(fun t -> t |> translateToTile tilemap WaterPipe)
    let deco = data.Decorations |> List.map(fun t -> t |> translateToTile tilemap Floor)
    let grating = data.Grating |> List.map(fun t -> t |> translateToTile tilemap Floor)
    let overlays = data.Overlays |> List.map(fun t -> t |> translateToTile tilemap Floor)
    let doors = data.Doors |> List.map(fun d -> d |> createDoor walls)

    let tiles = 
      floors 
      |> mergeWith airPipes 
      |> mergeWith waterPipes
      |> mergeWith grating 
      |> mergeWith deco 
      |> mergeWith walls 
      |> mergeWith overlays 
      |> Seq.toList

    let objects = 
      data.Objects 
      |> Seq.map(translateToObject) 
      |> Seq.append doors
      |> Seq.toList

    tiles 
    |> List.map(fun t -> {t with Objects=(objects |> getObjectsAtPos t.Pos |> Seq.toList)})
    |> create

  let loadWorld (filename: string) =
    let tiledFile = new TiledSharp.TmxMap(filename)
    tiledFile 
    |> interpretWorld
    |> generateWorld tiledFile
