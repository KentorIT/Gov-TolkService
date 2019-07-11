
$("body").on("click", ".a-region-contract", function () {
    $("#contract-per-region").show();
    $("#contract-per-broker").hide();
    $("#link-display-brok").show();
    $("#link-display-reg").hide();
});

$("body").on("click", ".a-broker-contract", function () {
    $("#contract-per-region").hide();
    $("#contract-per-broker").show();
    $("#link-display-brok").hide();
    $("#link-display-reg").show();
});