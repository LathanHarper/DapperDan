# Source from a workflow step. Only explicit, reviewed public proof profiles.
case "${FLEXLER_PROOF_CONFIGURATION:-}" in
  Debug|Release) ;;
  *) echo 'Select Debug or Release for the public proof.' >&2; return 1 ;;
esac

proof_msbuild_args=(-p:FlexlerShowcaseProof=true)
case "${FLEXLER_PROOF_RUNTIME_PROFILE:-host}" in
  host) ;;
  aot-trim)
    if [[ "$FLEXLER_PROOF_CONFIGURATION" != Release ]]; then
      echo 'The aot-trim diagnostic requires Release.' >&2
      return 1
    fi
    # Command-line global properties deliberately override the host's -all
    # fallback for this diagnostic only. This is Mono AOT, not NativeAOT.
    proof_msbuild_args+=(
      -p:UseInterpreter=false
      -p:MtouchInterpreter=
      -p:TrimMode=partial
      -p:UseMonoRuntime=true
      -p:PublishAot=false
      -p:MtouchUseLlvm=true
      -p:MauiXamlInflator=SourceGen
      -p:Registrar=managed-static
    )
    ;;
  *) echo 'Select host or aot-trim for the public runtime profile.' >&2; return 1 ;;
esac
