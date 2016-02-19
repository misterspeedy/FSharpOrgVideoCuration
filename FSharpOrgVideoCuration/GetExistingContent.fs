namespace FSharpOrgVideoCuration

(*

category (web, data-science, ...)
level (100, 200, 300, 400)
platform (Windows only? Cross platform?)
format (professional production v. hand-held webcam)
tags


*)

module GetExistingContent = 

    open System
    open System.Net
    open FSharp.Data

    type Video =
        {
            Url : string
            Image : string
            Description : string
            Live : bool
        }

    type Category = | Web | DataScience
    type Platform = | Windows | Mono | Linux | Mac
    type Format = | Pro | LoFi | Conference

    type AnnotatedVideo =
        {
            video : Video
            category : Category
            platform : Platform
            format : Format
            year : string
        }

    [<Literal>]
    let sampleUrl = @"http://fsharp.org/videos/1.html"

    type FSharpOrg = HtmlProvider<sampleUrl>

    type WebClientTimeout() =
        inherit WebClient()
        override __.GetWebRequest(uri : Uri) =
            let w = base.GetWebRequest(uri)
            w.Timeout <- 60000
            w

    let CheckLive (url : string) =
        use wc = new WebClientTimeout() 
        try
            // wc.OpenRead(url) |> ignore
            // wc.ResponseHeaders.["content-type"] |> ignore
            url |> wc.DownloadData |> ignore
            true
        with
        | _ -> false


    let GetExistingContent() =
        for i in 1..14 do
            let url = sprintf @"http://fsharp.org/videos/%i.html" i
            FSharpOrg.Load(url).Html.Body().Descendants()
            |> Seq.filter (fun e -> e.AttributeValue("class") = "thumbnail")
            |> Seq.map (fun e -> 
                e.AttributeValue("href"), 
                (e.Elements() |> Seq.tryFind (fun e' -> e'.Name() = "img")))
            |> Seq.choose (fun (u, im) ->
                match im with
                | Some img -> (u, img.AttributeValue("src"), img.AttributeValue("alt")) |> Some
                | None -> None)
            |> Seq.map (fun (u, i, d) ->
                {
                    Url = u
                    Image = i
                    Description = d
                    Live = CheckLive u
                }
            )
            |> Seq.filter (fun d -> d.Live |> not)
            |> Seq.iter (printfn "%A")
    ()

    do GetExistingContent()