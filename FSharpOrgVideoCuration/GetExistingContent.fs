#if COMPILED
namespace FSharpOrgVideoCuration
#endif

module GetExistingContent = 

    open System
    open System.Net
    open FSharp.Data

    type Video =
        {
            Url : string
            Image : string
            Description : string
            Existing : bool
            Live : bool
        }

    type Category = | Web | DataScience | Introduction 
    type Platform = | Windows | Mono | Linux | Mac
    type Format = | Pro | LoFi | Conference

    type AnnotatedVideo =
        {
            video : Video
            category : Category
            platform : Platform
            format : Format
            free : bool
            year : string
            tags : string[]
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
            url |> wc.DownloadData |> ignore
            true
        with
        | _ -> false


    let GetExistingContent() =
        [|
            for i in 1..14 do
                let url = sprintf @"http://fsharp.org/videos/%i.html" i
                let pageItems =
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
                            Existing = true
                        }
                    )
                    |> Seq.map (fun d -> sprintf "%s\t%s\t%s\t%b\t%b" d.Url d.Image d.Description d.Live d.Existing)
                    |> Array.ofSeq
                yield pageItems
        |]
        |> Array.concat

    let videos = GetExistingContent()
    IO.File.WriteAllLines(__SOURCE_DIRECTORY__ + "/Data/existing.tsv", videos)