﻿module Elovuon.Pairing
open System

let private getPrios lo hi weights =
  if List.isEmpty weights || weights |> List.exists (snd >> List.isEmpty) then List.empty else
  weights
  |> List.collect (fun (c,a) ->
     a
     |> List.choose (fun (w,(o,b)) ->
        if w < lo || w >= hi then None else
        let edge = Graph.edge c o
        Some ((w, -a.Length), ((w, edge), if fst edge = c then b else not b))
        ))
  |> Seq.sort
  |> List.ofSeq
  |> List.rev
  |> List.map snd
  |> Seq.distinct
  |> Seq.toList

let private weights2graph weights =
  weights
  |> List.collect (fun (c, a) -> a |> List.map (fun (_,(o,_)) -> Graph.edge c o))
  |> Seq.distinct
  |> Graph.fromEdges

let private limit we weights =
    weights
    |> List.map (fun (c, a) ->
        c, List.filter (fun (w,_) -> w >= we) a)

let private strip we (x,y) weights =
    weights
    |> List.choose (fun (c, a) ->
        if c = x || c = y then None else
        Some (c, List.filter (fun (w,(o,_)) -> w >= we && o <> x && o <> y) a))

let private minWeight<'weight when 'weight : comparison> maxValue (weights : (Contestant * ('weight * (Contestant * bool)) list) list) =
  let values =
    weights
    |> List.collect (snd >> (List.map fst))
    |> Seq.distinct
    |> Seq.sort
    |> Array.ofSeq

  let solves w =
    if w = values.[0] then true else
    let weights = weights |> limit w
    let m = weights |> List.map (snd >> List.length) |> List.max
    if weights |> List.exists (snd >> List.isEmpty) then false else
    weights
    |> weights2graph
    |> Graph.findPerfectMatching
    |> Option.isSome
    
  let rec inner lo hi =    
    if lo = hi - 1
    then
      values.[lo],
      if hi < values.Length then values.[hi] else maxValue
    else
    let mi = (hi + lo) / 2
    if solves values.[mi] then inner mi hi else inner lo mi
  let best = weights |> List.map (fun (_,a) -> fst a.[0]) |> List.min
  System.Array.BinarySearch(values, best) + 1
  |> inner 0

let getPairing<'weight when 'weight : comparison> minValue maxValue (weights : (Contestant * ('weight * (Contestant * bool)) list) list) =
//let getPairing minValue maxValue weights =
  let rec inner lower pairing =
    let paired = pairing |> List.collect (fun (o,c) -> [o;c]) |> set
    let weights =
      weights
      |> List.choose (fun (c, a) ->
         if paired.Contains c then None else
         Some (c, List.filter (fun (w,(o,_)) -> w >= lower && (paired.Contains o |> not)) a))
    if weights.Length = 0 then pairing else

    let rec sub we (x,y) =
      let getWeight (x,y) =
        weights
        |> List.find (fst >> (=) x)
        |> snd
        |> List.find (snd >> fst >> (=) y)
      let weights = strip we (x,y) weights
      if weights |> List.exists (snd >> List.isEmpty) then None else
      printf "."
      match weights2graph weights |> Graph.findPerfectMatching with
      | None -> None
      | Some matching ->
        let pairs =
          matching
          |> List.filter (fun (c,o) ->
             weights
             |> List.exists (fun (p, a) ->
                if p <> c && p <> o then false else
                let q = if p = c then o else c
                a
                |> Seq.takeWhile (fun (w,(x,_)) -> w < we)
                |> Seq.exists (fun (_,(x,_)) -> x = q)))
        List.length pairs + 1 |> printf "%d"
        (x,y)::pairs
        |> List.map (fun (x,y) ->
           let w,(_,black) = getWeight (x,y)
           if black then y,x else x,y)
        |> (@) pairing
        |> inner we
        |> Some

    match weights |> List.filter (snd >> List.length >> (=) 1) with
    | [] ->
      let mw,uw = minWeight maxValue weights
      match getPrios mw uw weights with
      | [ (_,(x,y)),black ] ->
        printf "-"
        (if black then y,x else x,y) :: pairing |> inner mw
      | prios -> prios |> List.map fst |> List.pick (fun (w,e) -> sub mw e)
    | a ->
      let pairs =
        a
        |> Seq.map (fun (c,w) -> let _,(o,b) = w.[0] in if b then o,c else c,o)
        |> Seq.distinct
        |> List.ofSeq
      List.length pairs |> printf "+%d"
      pairs @ pairing |> inner lower

  try
    inner minValue []
  finally
    printfn ""