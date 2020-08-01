﻿namespace MattEland.FSharpStarship.Logic

open GameObjects
open Tiles
open Positions
open Simulations
open World

module PlayerControl =

  type PlayerCommand =
    | Move of Direction
    | Wait

  let moveEntity originTile destinationTile tiles entity =
    let modifiedOrigin = originTile |> removeObject entity
    let modifiedDestination = destinationTile |> addObject entity 

    tiles 
    |> replaceTile modifiedOrigin
    |> replaceTile modifiedDestination
    
  let toggleDoor door targetTile tiles =
      let newTarget = targetTile |> replaceObject door (toggleDoorOpen door)
      tiles |> replaceTile newTarget
    
  let tryMoveEntity tile tiles dir entity =
    let newPos = tile.Pos |> offsetDir dir
    let targetTile = tiles |> getTile newPos

    match targetTile.Objects |> findClosedDoor with
    | Some door -> tiles |> toggleDoor door targetTile
    | _ ->    
      match targetTile.Flags.BlocksMovement with
      | true -> tiles
      | false -> entity |> moveEntity tile targetTile tiles

  let tryMovePlayer dir tiles =
    let tile = tiles |> List.tryFind(fun t -> t.Objects |> List.exists(fun o -> o.ObjectType = Astronaut))
    match tile with
    | Some playerTile -> 
        playerTile.Objects 
        |> List.find(fun o -> o.ObjectType = Astronaut)
        |> tryMoveEntity playerTile tiles dir
    | None -> 
        tiles

  let handlePlayerCommand command tiles =
    match command with
    | Move dir -> 
        tiles 
        |> tryMovePlayer dir 
        |> simulate
    | Wait -> 
        tiles 
        |> simulate
