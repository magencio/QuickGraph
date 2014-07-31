﻿module YC.FST.FstApproximation

open QuickGraph
open YC.FST.GraphBasedFst
open Microsoft.FSharp.Collections

type EdgeLblAppr<'br when 'br:comparison> = 
    | Smb of string * 'br
    | Repl of Appr<'br> * string * string
    | Trim of Appr<'br>

and [<Class>]Appr<'br when 'br:comparison>(initial, final, edges) as this =
    inherit AdjacencyGraph<int, TaggedEdge<int, EdgeLblAppr<'br>>>()
    do 
        edges |> ResizeArray.map (fun (f,l,t) -> new TaggedEdge<_,_>(f,t,l))
        |> this.AddVerticesAndEdgeRange
        |> ignore

    let toFSTGraph () =
        let resFST = new FST<_,_>()
        resFST.InitState <- this.InitState
        resFST.FinalState <- this.FinalState
        
        let counter = this.Vertices |> Seq.max |> ref

        let splitEdge (edg:TaggedEdge<int, EdgeLblAppr<'br>>) str br =
            let start = edg.Source
            let _end = edg.Target

            match str with
            | Some("") -> [|new TaggedEdge<_,_>(start, _end, new EdgeLbl<_,_>(Eps, Eps))|]
            | None -> [|new TaggedEdge<_,_>(start, _end, new EdgeLbl<_,_>(Eps, Eps))|]
            | Some(s) ->
                let l = s.Length
                let ss = s.ToCharArray()
                Array.init l 
                    (fun i ->
                        match i with
                        | 0 when (l = 1)     -> new TaggedEdge<_,_>(start, _end, new EdgeLbl<_,_>(Smbl (ss.[i], br), Smbl (ss.[i]))) 
                        | 0                  -> new TaggedEdge<_,_>(start, (incr counter; !counter), new EdgeLbl<_,_>(Smbl (ss.[i], br), Smbl (ss.[i]))) 
                        | i when (i = l - 1) -> new TaggedEdge<_,_>(!counter, _end, new EdgeLbl<_,_>(Smbl (ss.[i], br), Smbl (ss.[i]))) 
                        | i                  -> new TaggedEdge<_,_>(!counter, (incr counter; !counter), new EdgeLbl<_,_>(Smbl (ss.[i], br), Smbl (ss.[i]))) 
                    )

        let rec go (approximation:Appr<_>) =
            for edge in approximation.Edges do
                match edge.Tag with 
                | Smb (str, br) -> 
                    splitEdge edge (Some str) br
                    |> resFST.AddVerticesAndEdgeRange
                    |> ignore                                        
                | Repl (a,str1,str2) -> go a
                | Trim a -> go a
    
        let vEOF = !counter + 1
        for v in resFST.FinalState do
            new TaggedEdge<_,_>(v, vEOF, new EdgeLbl<_,_>(Smbl (char 65535,  Unchecked.defaultof<'br>), Smbl (char 65535))) |> resFST.AddVerticesAndEdge |> ignore
        resFST.FinalState <- ResizeArray.singleton vEOF
        go this
        resFST

    new () = 
        Appr<_>(new ResizeArray<_>(),new ResizeArray<_>(),new ResizeArray<_>())
             
    member val InitState =  initial with get, set
    member val FinalState = final with get, set
    member this.ToFST () = toFSTGraph()

