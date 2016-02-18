﻿module Common

open System


let unreachable () = 
    failwith "unreachable"

module Debug =

    let debug n f =
        let settings = Args.getSettings ()
        if settings.Debug >= n then
            f ()

    let debug1 f = debug 1 f
    let debug2 f = debug 2 f
    let debug3 f = debug 3 f

    let logInfo n idx str =
        let settings = Args.getSettings ()
        let logFile = settings.DebugDir + "debug(" + string idx + ").log"
        let indent = String.replicate (n-1) "\t"
        if settings.Debug >= n then
            System.IO.File.AppendAllText(logFile, indent + str + "\n")

    let logInfo1(idx, f) = logInfo 1 idx f
    let logInfo2(idx, f) = logInfo 2 idx f
    let logInfo3(idx, f) = logInfo 3 idx f


module Profile =

    let time f x = 
        let s = System.Diagnostics.Stopwatch()
        s.Start()
        let ret = f x
        s.Stop()
        (ret, s.ElapsedMilliseconds)


module List = 

    let inline fold f b ls = 
        let mutable acc = b 
        for i in ls do 
            acc <- f acc i 
        acc

    let inline fold1 f ls = 
        match ls with 
        | [] -> failwith "empty list in fold1"
        | hd::tl -> 
            let mutable acc = hd 
            for i in tl do 
                acc <- f acc i 
            acc 

    let inline joinBy sep ss = 
        fold1 (fun a b -> a + sep + b) ss

    let inline toString xs = 
        match xs with 
        | [] -> "[]"
        | _ -> 
            let s = joinBy "," (List.map string xs)
            sprintf "[%s]" s

    let combinations n ls = 
        let rec aux acc size set = seq {
            match size, set with 
            | n, x::xs -> 
                if n > 0 then yield! aux (x::acc) (n - 1) xs
                if n >= 0 then yield! aux acc n xs 
            | 0, [] -> yield acc 
            | _, [] -> () }
        aux [] n ls


module Set = 

    let inline fold1 f xs = 
        if Set.isEmpty xs then 
            failwith "empty set in fold1"
        else 
            let x = Set.minElement xs
            let xs' = Set.remove x xs
            Set.fold f x xs'

    let inline joinBy sep ss =
        fold1 (fun a b -> a + sep + b) ss

    let inline toString ss = 
        if Set.isEmpty ss then "{}"
        else 
            let s = joinBy "," (Set.map string ss)
            sprintf "{%s}" s


module Option =

    let inline getOrDefault d o = 
        match o with
        | None -> d
        | Some x -> x


module Map = 

    let inline getOrDefault k d m = 
        match Map.tryFind k m with 
        | None -> d 
        | Some x -> x

    let inline adjust k d f m = 
        let current = getOrDefault k d m
        Map.add k (f current) m

    let merge a b f =
        Map.fold (fun s k v ->
            match Map.tryFind k s with
            | Some v' -> Map.add k (f k (v, v')) s
            | None -> Map.add k v s) b a


module Error =

    type Result<'a, 'b> = 
        | Ok of 'a
        | Err of 'b

    let isOk res = 
        match res with 
        | Ok _ -> true 
        | Err _ -> false 

    let isErr res =
        match res with 
        | Ok _ -> false 
        | Err _ -> true

    let unwrap res = 
        match res with 
        | Ok v -> v 
        | Err _ -> failwith "unwrapping error result"

    let map f res = 
        match res with 
        | Ok v -> Ok (f v)
        | Err e -> Err e


module Format =

    let obj = new Object()

    let footerSize = 80

    let wrapText offset (s: string) : string = 
        let s = s.Trim()
        let words = s.Split(' ')
        let mutable count = offset 
        let mutable result = ""
        for word in words do
            let len = word.Length
            if count + len + 2 > footerSize then 
                let spaces = String.replicate offset " "
                result <- result + "\n" + spaces + word
                count <- word.Length + offset
            else 
                let space = (if result = "" then "" else " ")
                result <- result + space + word
                count <- count + len + 1
        result

    let writeColor (s: string) c = 
        Console.ForegroundColor <- c
        Console.Write s
        Console.ResetColor ()

    let writeHeader () =
        let settings = Args.getSettings () 
        let name = Option.get settings.PolFile
        writeColor name ConsoleColor.DarkCyan
        printfn ""

    let writeFooter () =
        printfn "%s" (String.replicate footerSize "-")

    let error str = 
        lock obj (fun () ->
            writeHeader ()
            printfn ""
            let s = "Error: "
            writeColor s ConsoleColor.DarkRed
            printfn "%s" (wrapText s.Length str)
            writeFooter ()
            exit 0)

    let warning str = 
        lock obj (fun () ->
            writeHeader ()
            printfn ""
            let s = "Warning: "
            writeColor s ConsoleColor.DarkYellow
            printfn "%s" (wrapText s.Length str)
            writeFooter ())

    let inline cyan (s: string) = 
        lock obj (fun () -> writeColor s ConsoleColor.DarkCyan)

    let inline green (s: string) =
        lock obj (fun () -> writeColor s ConsoleColor.DarkGreen)

    let inline red (s: string) = 
        lock obj (fun () -> writeColor s ConsoleColor.DarkRed)

    let inline gray (s: string) = 
        lock obj (fun () -> writeColor s ConsoleColor.DarkGray)

    let writeFormatted (s:string) =
        let arr = s.Split('#')
        if arr.Length <= 2 then
            printf "%s" s
        for s in arr.[0..(arr.Length - 1)] do 
            if s.Length >= 6 && s.[0..4] = "(red)" then red s.[5..]
            elif s.Length >= 8 && s.[0..6] = "(green)" then green s.[7..]
            elif s.Length >= 7 && s.[0..5] = "(gray)" then gray s.[6..]
            elif s.Length >= 7 && s.[0..5] = "(cyan)" then cyan s.[6..]
            else lock obj (fun () -> printf "%s" s)

    let passed () = 
        writeFormatted "#(green)passed#\n"

    let failed () = 
        writeFormatted "#(red)failed#\n" 

    




