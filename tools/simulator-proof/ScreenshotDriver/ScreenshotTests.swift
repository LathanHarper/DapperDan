import XCTest
import UIKit

// A tiny native UI driver for an already-installed MAUI app. No app target or
// production code is built here; XCTest supplies real device orientation events.
final class ScreenshotTests: XCTestCase {
    func testPortrait() { capture(.portrait, name: "portrait") }
    func testLandscape() { capture(.landscapeLeft, name: "landscape") }

    private func capture(_ orientation: UIDeviceOrientation, name: String) {
        continueAfterFailure = false
        let app = XCUIApplication(bundleIdentifier: "net.codecrafty.dapperdan")
        app.activate()
        let action = app.descendants(matching: .any)
            .matching(identifier: "Squishy_Alpha").firstMatch
        XCTAssertTrue(action.waitForExistence(timeout: 45), "Squishy canary did not open")
        XCUIDevice.shared.orientation = orientation
        let rotated = XCTNSPredicateExpectation(predicate: NSPredicate { _, _ in
            name == "portrait" ? app.frame.height > app.frame.width : app.frame.width > app.frame.height
        }, object: nil)
        XCTAssertEqual(XCTWaiter.wait(for: [rotated], timeout: 15), .completed)
        XCTAssertTrue(action.isHittable, "Action is not visible after rotation")
        // Permit the app's read-only 400 ms geometry recorder to settle.
        RunLoop.current.run(until: Date(timeIntervalSinceNow: 1))
        let screenshot = XCTAttachment(screenshot: XCUIScreen.main.screenshot())
        screenshot.name = "dapper-\(name)"
        screenshot.lifetime = .keepAlways
        add(screenshot)
        let tree = XCTAttachment(string: app.debugDescription)
        tree.name = "dapper-\(name)-ui-tree"
        tree.lifetime = .keepAlways
        add(tree)
    }
}
