#!/usr/bin/env python3
import argparse
import functools
import http.server
import os


class UnityWebGLRequestHandler(http.server.SimpleHTTPRequestHandler):
    def guess_type(self, path):
        uncompressed_path = path
        if path.endswith(".br"):
            uncompressed_path = path[:-3]
        elif path.endswith(".gz"):
            uncompressed_path = path[:-3]

        if uncompressed_path.endswith(".wasm"):
            return "application/wasm"
        if uncompressed_path.endswith(".js"):
            return "application/javascript"
        if uncompressed_path.endswith(".data"):
            return "application/octet-stream"
        return super().guess_type(uncompressed_path)

    def end_headers(self):
        request_path = self.path.split("?", 1)[0]
        if request_path.endswith(".br"):
            self.send_header("Content-Encoding", "br")
        elif request_path.endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")

        self.send_header("Cache-Control", "no-store")
        super().end_headers()


def main():
    parser = argparse.ArgumentParser(description="Serve a Unity WebGL build locally.")
    parser.add_argument("directory", help="Directory containing index.html")
    parser.add_argument("--port", type=int, default=8000)
    parser.add_argument("--bind", default="127.0.0.1")
    args = parser.parse_args()

    build_directory = os.path.abspath(args.directory)
    handler = functools.partial(UnityWebGLRequestHandler, directory=build_directory)
    server = http.server.ThreadingHTTPServer((args.bind, args.port), handler)
    print(f"Serving {build_directory} at http://{args.bind}:{args.port}")
    server.serve_forever()


if __name__ == "__main__":
    main()
